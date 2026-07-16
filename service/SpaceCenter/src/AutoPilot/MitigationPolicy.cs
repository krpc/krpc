using System;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// The gating layer of the flexible-craft handling: maps the detector signals
    /// (<see cref="OscillationDetectors"/>) to per-tick mitigation decisions — which rate-filter
    /// tool runs at what frequency, how strongly each mitigation is engaged (the eased latch
    /// ramp and the hold-gated mitigation level), and the output-filter corner/weight choice.
    /// All the routing thresholds and gating time constants live here; the mitigation
    /// primitives (<see cref="RateFilter"/>, <see cref="OutputFilter"/>, the bandwidth floor in
    /// the autotuner and the feedforward cut) only consume its outputs, so each mitigation can
    /// be driven manually by bypassing this class for that primitive alone.
    /// </summary>
    sealed class MitigationPolicy
    {
        // Routing threshold (Hz): a detected mode below this is well-conditioned for a notch
        // (K = tan(π·f·dt) < 1 up to 12.5 Hz at 50 Hz physics) and close enough to the band that
        // a low-pass would add too much crossover phase lag; above it the notch degrades toward
        // the Nyquist singularity and the low-pass is the right tool. Set to 12 Hz on measured
        // data: the stock flexible test craft sit at ~2.5–8.5 Hz (notch) with only near-Nyquist
        // roll modes (~21–25 Hz) on the low-pass.
        internal const double SplitFrequency = 12.0;
        // Low-pass corner separation: the high-frequency-branch corner is f_detected / L_lp,
        // clamped to LowPassCornerMin (Hz) so it never drifts down into the sub-Hz control band.
        internal const double LowPassSeparation = 3.0;
        internal const double LowPassCornerMin = 2.0;
        // Time constant of the one-pole ramp easing the latched mitigations in/out.
        const double RampTimeConstant = 0.5;
        // Control-output oscillation envelope (the runtime analogue of the test suite's
        // control_oscillation_amplitude) above which a latched axis is treated as still
        // limit-cycling, so the hold mitigation engages regardless of pointing error. A settled
        // hold sits near 0.008 and a limit cycle saturates toward ~1, so 0.2 has wide margin.
        internal const double EnvelopeThreshold = 0.2;

        // Per-axis one-pole ramp [0,1] easing the latched mitigations in/out so the control does
        // not step when suppression engages/releases.
        readonly double[] suppressionRamp = new double[3];
        // Automatic (envelope-driven) component of the oscillation-control back-off: the
        // per-axis ramp that rises while a latched axis limit-cycles and decays slowly when
        // quiet. The manual override level is a floor under it.
        readonly double[] oscControlAuto = new double[3];
        readonly double[] oscControlBackoff = new double[3];
        // Per-axis hold-gated mitigation weight (suppressionRamp · max(holdFactor, backoff)):
        // how fully the latched hold mitigation (bandwidth floor + feedforward cut + nominal
        // target) is engaged. The floor follows this, not suppressionRamp, so a latched axis
        // runs at full bandwidth while slewing and is floored only while holding (or while the
        // back-off reports a limit cycle).
        readonly double[] mitigationLevel = new double[3];

        public double SuppressionRamp (int index)
        {
            return suppressionRamp [index];
        }

        public double Backoff (int index)
        {
            return oscControlBackoff [index];
        }

        public double MitigationLevel (int index)
        {
            return mitigationLevel [index];
        }

        public void Reset ()
        {
            for (int i = 0; i < 3; i++) {
                suppressionRamp [i] = 0;
                oscControlAuto [i] = 0;
                oscControlBackoff [i] = 0;
                mitigationLevel [i] = 0;
            }
        }

        /// <summary>
        /// Select the suppression tool and its frequency for an axis group. <c>tool</c> is 0
        /// (none), 1 (notch) or 2 (low-pass). In <c>Automatic</c> mode suppression is active
        /// only while the group is latched, and is routed by the estimated frequency (notch
        /// below <see cref="SplitFrequency"/>, low-pass above), with a frequency-independent
        /// broadband low-pass fallback while the estimator has not locked — the estimator is
        /// unreliable on some craft, so this fallback (plus the gain-stabilising bandwidth
        /// floor) is what makes suppression robust without a confident estimate.
        /// <c>Notch</c>/<c>LowPass</c> force that tool at the manual frequency; <c>Off</c>
        /// applies nothing.
        /// </summary>
        public static void SelectTool (Services.RateFilterMode mode, bool latched,
            double detectedHz, double manualHz, out int tool, out double freq)
        {
            switch (mode) {
            case Services.RateFilterMode.Notch:
                tool = 1;
                freq = manualHz;
                break;
            case Services.RateFilterMode.LowPass:
                tool = 2;
                freq = manualHz;
                break;
            case Services.RateFilterMode.Off:
                tool = 0;
                freq = 0;
                break;
            default:  // Automatic
                if (!latched) {
                    tool = 0;
                    freq = 0;
                } else if (double.IsNaN (detectedHz)) {
                    tool = 2;
                    freq = LowPassCornerMin * LowPassSeparation;
                } else {
                    freq = detectedHz;
                    tool = freq < SplitFrequency ? 1 : 2;
                }
                break;
            }
        }

        /// <summary>
        /// Ease the latched mitigations in/out per axis, gated on the rate filter's
        /// suppression-active record — the persistent latch, not the decaying chatter level —
        /// so they hold for as long as the craft is treated as flexible and do not step at
        /// engage.
        /// </summary>
        public void UpdateRamps (RateFilter rateFilter, double dt)
        {
            var rampBeta = 1.0 - Math.Exp (-dt / RampTimeConstant);
            for (int i = 0; i < 3; i++)
                suppressionRamp [i] +=
                    rampBeta * ((rateFilter.SuppressionActive (i) ? 1.0 : 0.0) - suppressionRamp [i]);
        }

        /// <summary>
        /// Compute the per-axis hold-gated mitigation level (the "gate"): suppressionRamp ·
        /// max(holdFactor, oscillation-control back-off). The back-off is the hold gate's
        /// blind-spot closure: a manoeuvre that pushes the pointing error above the hold band
        /// would release the mitigation, which on a flexible craft can re-excite the bending
        /// mode into a limit cycle that parks the error across the band. The trigger is the
        /// detectors' about-mean delivered-command envelope — a sustained limit cycle has a
        /// large envelope while a steady slew does not — rising fast / decaying slow, only on a
        /// latched axis. Pitch (x) and yaw (z) are coupled (mirroring the chatter detector) and
        /// use <paramref name="pitchYawHold"/>; roll (y) is on its own and uses
        /// <paramref name="rollHold"/>, which its caller keys on the roll error as well as the
        /// pointing error so a pure roll maneuver releases the roll mitigation.
        /// </summary>
        public Vector3d UpdateGate (OscillationDetectors detectors, double dt,
            double pitchYawHold, double rollHold)
        {
            var controlEnv = detectors.ControlOscEnvelope;
            var pyEnv = Math.Max (controlEnv.x, controlEnv.z);
            var groupEnv = new Vector3d (pyEnv, controlEnv.y, pyEnv);
            for (int i = 0; i < 3; i++) {
                var oscTarget = detectors.ChatterLatched (i) && groupEnv [i] > EnvelopeThreshold ? 1.0 : 0.0;
                var tc = oscTarget > oscControlAuto [i]
                    ? OscillationDetectors.ChatterRiseTimeConstant
                    : OscillationDetectors.ChatterDecayTimeConstant;
                var beta = 1.0 - Math.Exp (-dt / tc);
                oscControlAuto [i] += beta * (oscTarget - oscControlAuto [i]);
                oscControlBackoff [i] = oscControlAuto [i];
            }
            var gate = new Vector3d (
                suppressionRamp [0] * Math.Max (pitchYawHold, oscControlBackoff [0]),
                suppressionRamp [1] * Math.Max (rollHold, oscControlBackoff [1]),
                suppressionRamp [2] * Math.Max (pitchYawHold, oscControlBackoff [2]));
            mitigationLevel [0] = gate.x;
            mitigationLevel [1] = gate.y;
            mitigationLevel [2] = gate.z;
            return gate;
        }

        /// <summary>
        /// The output-filter decision for one axis: a latched axis uses the ramped suppression
        /// weight at the heavier corner (the flexible-craft path); an unlatched axis whose
        /// detector is firing blends by chatterLevel at the lighter corner (smoothing the
        /// delivered command without the phase cost of the heavy corner destabilising the
        /// full-bandwidth loop); a rigid axis gets weight 0 and passes through.
        /// </summary>
        public void ChooseOutputFilter (OscillationDetectors detectors, int index,
            out double tau, out double weight)
        {
            weight = detectors.ChatterLatched (index) ? suppressionRamp [index] : detectors.ChatterLevel [index];
            tau = detectors.ChatterLatched (index)
                ? OutputFilter.LatchedTimeConstant
                : OutputFilter.DetectorTimeConstant;
        }
    }
}
