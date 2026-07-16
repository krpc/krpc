using System;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// The observation layer of the flexible-craft handling: every signal the mitigation policy
    /// gates on, with no control-path side effects. Owns the structural-chatter detector (level +
    /// per-engagement latch), the online frequency trackers with their high-pass bookkeeping and
    /// held estimates, the control-output oscillation envelope, and the pointing-error hold
    /// factor. Fed the raw measured rate and the delivered command each tick by
    /// <see cref="AttitudeController"/>; consumed by the mitigation policy and the public
    /// observables.
    /// </summary>
    sealed class OscillationDetectors
    {
        // One-sided smoothing time constants for the chatter level: engage quickly when
        // excitation appears, release slowly so the loop does not hunt (the mitigated state is
        // quiet, which would otherwise immediately clear the detector).
        internal const double ChatterRiseTimeConstant = 0.3;
        internal const double ChatterDecayTimeConstant = 30.0;
        // chatterLevel above which an axis is latched into full mitigation for the rest of the
        // engagement. Set below the level a confirmed limit cycle reaches (saturates toward 1)
        // but well above any transient a rigid craft produces.
        internal const double ChatterLatchThreshold = 0.6;
        // Time constant of the slow mean subtracted from the raw rate to high-pass it for the
        // frequency trackers (~0.5 Hz corner): well below the structural modes (≥1 Hz) so they
        // pass, but high enough to remove DC and slew trends.
        const double FrequencyHighpassTimeConstant = 0.3;
        // Time constant of the slow per-axis envelope of |high-passed ω| used to pick the
        // stronger transverse axis for the pitch/yaw group estimate.
        const double AbsHighpassEnvelopeTimeConstant = 0.5;
        // Time constant of the slow "trim" mean subtracted from the delivered command to form
        // the about-mean envelope: long enough to track a steady slew (so a one-sign ramp is not
        // counted as oscillation) yet shorter than the limit-cycle period (~0.7 s).
        const double OscControlMeanTimeConstant = 0.5;
        // Time constant smoothing the |command − trim| envelope (a few cycles).
        const double OscControlEnvelopeTimeConstant = 0.3;
        // Pointing-error band (degrees) for the continuous hold factor: 1 while holding
        // (error ≤ HoldErrorFull), 0 while slewing (≥ HoldErrorNone), linear between.
        const double HoldErrorFull = 1.0;
        const double HoldErrorNone = 2.5;

        // Chatter-detector state. chatterLevel is a per-axis [0,1] measure of how strongly an
        // axis is in a structural limit cycle; chatterLatched is the per-engagement memory that
        // "this craft is flexible" (cleared only in Reset; the level persists across
        // re-engagements, decaying at τ = 30 s).
        Vector3d prevDetectorOmega;
        bool prevDetectorOmegaValid;
        Vector3d chatterLevel = Vector3d.zero;
        readonly bool[] chatterLatched = new bool[3];
        // Per-axis trigger margin of the last chatter sample: |Δω| / (threshold·α·dt), the
        // ratio the detector fires on (≥ 1 counts as excitation). Recorded for the diagnostic
        // log so how close each tick came to firing is visible, not just the smoothed level.
        Vector3d chatterMargin = Vector3d.zero;
        // Online frequency estimators, fed the high-passed rate every tick, and the sticky held
        // estimate per group (latched to the last acquired value so suppression quieting the
        // mode does not lose it; NaN only until first acquisition).
        readonly FrequencyTracker pitchYawFreqTracker = new FrequencyTracker ();
        readonly FrequencyTracker rollFreqTracker = new FrequencyTracker ();
        double pitchYawHeldHz = double.NaN;
        double rollHeldHz = double.NaN;
        // High-pass bookkeeping for the trackers (slow mean of ω, envelope of |high-passed ω|).
        Vector3d emaOmega;
        bool emaOmegaValid;
        Vector3d emaAbsHp = Vector3d.zero;
        // Previous tick's delivered command and the slow trim mean plus the about-mean envelope
        // built from it one tick late (the runtime analogue of the test suite's
        // control_oscillation_amplitude).
        Vector3d prevControl;
        bool prevControlValid;
        Vector3d controlMean;
        bool controlMeanValid;
        Vector3d controlOscEnvelope = Vector3d.zero;

        public Vector3d ChatterLevel {
            get { return chatterLevel; }
        }

        public Vector3d ChatterMargin {
            get { return chatterMargin; }
        }

        public bool ChatterLatched (int index)
        {
            return chatterLatched [index];
        }

        public bool PitchYawLatched {
            get { return chatterLatched [0] || chatterLatched [2]; }
        }

        public bool RollLatched {
            get { return chatterLatched [1]; }
        }

        public double PitchYawHeldHz {
            get { return pitchYawHeldHz; }
        }

        public double RollHeldHz {
            get { return rollHeldHz; }
        }

        // The trackers' live estimates (NaN whenever the mode is not currently acquired) and
        // acquisition progress, alongside the sticky held values above — for the diagnostic
        // log, so acquisition/loss dynamics are visible.
        public double PitchYawLiveHz {
            get { return pitchYawFreqTracker.EstimatedHz; }
        }

        public double RollLiveHz {
            get { return rollFreqTracker.EstimatedHz; }
        }

        public int PitchYawAgreeCount {
            get { return pitchYawFreqTracker.AgreeCount; }
        }

        public int RollAgreeCount {
            get { return rollFreqTracker.AgreeCount; }
        }

        public Vector3d ControlOscEnvelope {
            get { return controlOscEnvelope; }
        }

        /// <summary>
        /// Reset per-engagement state. chatterLevel is deliberately NOT reset — the level
        /// persists across re-engagements (decaying at τ = 30 s); only the latch is
        /// per-engagement. A full reset clears the level too, via
        /// <see cref="ResetChatterLevel"/>.
        /// </summary>
        public void Reset ()
        {
            prevDetectorOmegaValid = false;
            chatterMargin = Vector3d.zero;
            emaOmegaValid = false;
            emaAbsHp = Vector3d.zero;
            prevControlValid = false;
            controlMeanValid = false;
            controlOscEnvelope = Vector3d.zero;
            pitchYawFreqTracker.Reset ();
            rollFreqTracker.Reset ();
            pitchYawHeldHz = double.NaN;
            rollHeldHz = double.NaN;
            for (int i = 0; i < 3; i++)
                chatterLatched [i] = false;
        }

        /// <summary>
        /// Detect a per-axis structural limit cycle and drive <c>chatterLevel</c> in [0,1].
        /// </summary>
        /// <remarks>
        /// The signature of a bending-mode limit cycle is the measured rate (sampled at the root
        /// part) changing tick-to-tick by more than the available torque could physically
        /// produce: rigid-body motion is bounded by <c>α·dt</c> at full authority, so a jump
        /// several times larger is structural oscillation, not a response to the controller.
        /// Gain-independent and maneuver-independent. The level rises quickly and decays slowly;
        /// crossing <see cref="ChatterLatchThreshold"/> latches the axis as flexible for the
        /// engagement. A heavy, low-authority craft *can* latch on benign measurement jitter
        /// (the bound k·α·dt is authority-relative) and no measured signal separates that from a
        /// genuine bending mode (see the latch-discrimination design doc), so the latch is
        /// deliberately permissive and the latched mitigation itself is required to be benign on
        /// a craft that did not need it. Pitch (0) and yaw (2) latch together: a long vehicle's
        /// first lateral bending mode is essentially axisymmetric, so once either transverse
        /// axis confirms flexible the craft is flexible.
        /// </remarks>
        public void UpdateChatter (Vector3d rawOmega, Vector3d torque, Vector3d moi, double dt,
            double detectionThreshold)
        {
            if (!prevDetectorOmegaValid) {
                prevDetectorOmega = rawOmega;
                prevDetectorOmegaValid = true;
                return;
            }
            for (int i = 0; i < 3; i++) {
                var alpha = moi [i] > 0 ? torque [i] / moi [i] : 0.0;
                var deltaOmega = Math.Abs (rawOmega [i] - prevDetectorOmega [i]);
                chatterMargin [i] = alpha > 0 && dt > 0
                    ? deltaOmega / (detectionThreshold * alpha * dt) : 0.0;
                var excited = alpha > 0 && deltaOmega > detectionThreshold * alpha * dt ? 1.0 : 0.0;
                var timeConstant = excited > chatterLevel [i] ? ChatterRiseTimeConstant : ChatterDecayTimeConstant;
                var beta = 1.0 - Math.Exp (-dt / timeConstant);
                chatterLevel [i] += beta * (excited - chatterLevel [i]);
                if (chatterLevel [i] >= ChatterLatchThreshold)
                    chatterLatched [i] = true;
            }
            if (chatterLatched [0] || chatterLatched [2])
                chatterLatched [0] = chatterLatched [2] = true;
            prevDetectorOmega = rawOmega;
        }

        /// <summary>
        /// Clear the persistent chatter level, on top of what <see cref="Reset"/> clears. Kept
        /// out of Reset so the level survives re-engagements (a craft known to be flexible
        /// re-latches quickly); called only by the full controller reset — the user-facing
        /// return to initial conditions.
        /// </summary>
        public void ResetChatterLevel ()
        {
            chatterLevel = Vector3d.zero;
        }

        /// <summary>
        /// Force an axis rigid for control purposes: clear the latch and level so no suppression
        /// is applied. The frequency estimator keeps running so the detected-frequency
        /// observable stays live. (The detector side of the manual Off override.)
        /// </summary>
        public void ForceRigid (int index)
        {
            chatterLevel [index] = 0;
            chatterLatched [index] = false;
        }

        /// <summary>
        /// Feed the frequency trackers the oscillation in the raw rate — unconditionally, so an
        /// estimate is always warm the moment an axis latches. The rate is high-passed (slow
        /// mean subtracted) so the trackers see the structural oscillation without DC or slew
        /// trends, and without the high-frequency bias a raw Δω (derivative) would impose. The
        /// pitch/yaw tracker is fed whichever transverse axis currently carries more oscillation
        /// (larger envelope, the lateral mode being ~axisymmetric); roll is fed directly. The
        /// held estimates latch the last acquired value.
        /// </summary>
        public void UpdateTrackers (Vector3d rawOmega, double dt)
        {
            if (!emaOmegaValid) {
                emaOmega = rawOmega;
                emaOmegaValid = true;
            }
            var emaOmegaBeta = 1.0 - Math.Exp (-dt / FrequencyHighpassTimeConstant);
            emaOmega = new Vector3d (
                emaOmega.x + emaOmegaBeta * (rawOmega.x - emaOmega.x),
                emaOmega.y + emaOmegaBeta * (rawOmega.y - emaOmega.y),
                emaOmega.z + emaOmegaBeta * (rawOmega.z - emaOmega.z));
            var hp = rawOmega - emaOmega;
            var betaAbsHp = 1.0 - Math.Exp (-dt / AbsHighpassEnvelopeTimeConstant);
            emaAbsHp = new Vector3d (
                emaAbsHp.x + betaAbsHp * (Math.Abs (hp.x) - emaAbsHp.x),
                emaAbsHp.y + betaAbsHp * (Math.Abs (hp.y) - emaAbsHp.y),
                emaAbsHp.z + betaAbsHp * (Math.Abs (hp.z) - emaAbsHp.z));
            var pyHp = emaAbsHp.x >= emaAbsHp.z ? hp.x : hp.z;
            pitchYawFreqTracker.Update (pyHp, dt);
            rollFreqTracker.Update (hp.y, dt);
            if (!double.IsNaN (pitchYawFreqTracker.EstimatedHz))
                pitchYawHeldHz = pitchYawFreqTracker.EstimatedHz;
            if (!double.IsNaN (rollFreqTracker.EstimatedHz))
                rollHeldHz = rollFreqTracker.EstimatedHz;
        }

        /// <summary>
        /// Continuous hold factor: 1 while holding (pointing error ≤ HoldErrorFull), 0 while
        /// slewing (≥ HoldErrorNone), linear between. A smooth function of error — no
        /// hysteresis state machine.
        /// </summary>
        public static double HoldFactor (double pointingErrorDeg)
        {
            return Math.Min (1.0, Math.Max (0.0,
                (HoldErrorNone - pointingErrorDeg) / (HoldErrorNone - HoldErrorFull)));
        }

        /// <summary>
        /// Update the about-mean envelope of the delivered command (built from the previous
        /// tick's command, since this tick's does not exist yet when the gate consumes it): a
        /// sustained limit cycle has a large envelope while a steady slew (one-sign ramp,
        /// tracked by the trim mean) does not.
        /// </summary>
        public void UpdateControlEnvelope (double dt)
        {
            if (!prevControlValid)
                return;
            if (!controlMeanValid) {
                controlMean = prevControl;
                controlMeanValid = true;
            }
            var meanBeta = 1.0 - Math.Exp (-dt / OscControlMeanTimeConstant);
            controlMean += meanBeta * (prevControl - controlMean);
            var envBeta = 1.0 - Math.Exp (-dt / OscControlEnvelopeTimeConstant);
            controlOscEnvelope = new Vector3d (
                controlOscEnvelope.x + envBeta * (Math.Abs (prevControl.x - controlMean.x) - controlOscEnvelope.x),
                controlOscEnvelope.y + envBeta * (Math.Abs (prevControl.y - controlMean.y) - controlOscEnvelope.y),
                controlOscEnvelope.z + envBeta * (Math.Abs (prevControl.z - controlMean.z) - controlOscEnvelope.z));
        }

        /// <summary>Store the delivered command for next tick's envelope.</summary>
        public void RecordControl (Vector3d delivered)
        {
            prevControl = delivered;
            prevControlValid = true;
        }
    }
}
