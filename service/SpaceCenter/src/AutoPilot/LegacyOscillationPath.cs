using System;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// TEMPORARY shadow-verification scaffold for the oscillation-machinery refactor (see
    /// doc/design/autopilot-control-loop-redesign.md, Phase 2). A verbatim copy of the
    /// pre-refactor oscillation path — detector, trackers, suppression, ramps, envelope, gate,
    /// output smoothing — with its own state. Each tick <see cref="AttitudeController"/> feeds it
    /// the same inputs as the live path and compares every stage output exactly; any divergence
    /// is recorded (first stage + running max) and surfaced in the diagnostic log as
    /// <c>shadow=</c>. Both paths start from identical reset state and are fed identical inputs,
    /// so their outputs are bit-identical unless a refactor commit changed behavior.
    /// Deleted, together with the comparison calls, once the refactor is verified.
    /// </summary>
    sealed class LegacyOscillationPath
    {
        // ---- constants (verbatim copies of the pre-refactor AttitudeController values) ----
        const double DefaultOutputFilterTimeConstant = 0.08;
        const double DetectorOutputFilterTimeConstant = 0.035;
        const double DetectorRateFilterTimeConstant = 0.03;
        const double HoldErrorFull = 1.0;
        const double HoldErrorNone = 2.5;
        const double DefaultSplitFrequency = 12.0;
        const double DefaultLowPassSeparation = 3.0;
        const double LowPassCornerMin = 2.0;
        const double BandwidthRampTimeConstant = 0.5;
        const double FrequencyHighpassTimeConstant = 0.3;
        const double ChatterRiseTimeConstant = 0.3;
        const double ChatterDecayTimeConstant = 30.0;
        const double ChatterLatchThreshold = 0.6;
        const double OscControlMeanTimeConstant = 0.5;
        const double OscControlEnvelopeTimeConstant = 0.3;

        // ---- state (verbatim copies) ----
        readonly double[] rateFilterStage1 = new double[3];
        readonly double[] rateFilterStage2 = new double[3];
        readonly bool[] rateFilterValid = new bool[3];
        readonly double[] outputFilterState = new double[3];
        readonly bool[] outputFilterValid = new bool[3];
        readonly double[] rateInputFilterState = new double[3];
        readonly bool[] rateInputFilterValid = new bool[3];
        readonly BiquadNotchFilter pitchNotch = new BiquadNotchFilter ();
        readonly BiquadNotchFilter rollNotch = new BiquadNotchFilter ();
        readonly BiquadNotchFilter yawNotch = new BiquadNotchFilter ();
        readonly FrequencyTracker pitchYawFreqTracker = new FrequencyTracker ();
        readonly FrequencyTracker rollFreqTracker = new FrequencyTracker ();
        double pitchYawHeldHz = double.NaN;
        double rollHeldHz = double.NaN;
        Vector3d emaOmega;
        bool emaOmegaValid;
        Vector3d emaAbsHp = Vector3d.zero;
        readonly bool[] notchActiveAxis = new bool[3];
        readonly bool[] suppressionActiveAxis = new bool[3];
        readonly double[] suppressionRamp = new double[3];
        readonly double[] mitigationLevel = new double[3];
        readonly double[] oscControlBackoff = new double[3];
        readonly double[] oscControlAuto = new double[3];
        Vector3d prevControl;
        bool prevControlValid;
        Vector3d controlMean;
        bool controlMeanValid;
        Vector3d controlOscEnvelope = Vector3d.zero;
        Vector3d prevDetectorOmega;
        bool prevDetectorOmegaValid;
        Vector3d chatterLevel = Vector3d.zero;
        readonly bool[] chatterLatched = new bool[3];

        // ---- observables for the comparison ----
        public Vector3d ChatterLevel { get { return chatterLevel; } }
        public bool ChatterLatched (int i) { return chatterLatched [i]; }
        public double PitchYawHeldHz { get { return pitchYawHeldHz; } }
        public double RollHeldHz { get { return rollHeldHz; } }
        public Vector3d ControlOscEnvelope { get { return controlOscEnvelope; } }
        public double MitigationLevel (int i) { return mitigationLevel [i]; }
        public bool NotchActive (int i) { return notchActiveAxis [i]; }
        public double SuppressionRamp (int i) { return suppressionRamp [i]; }

        /// <summary>Reset all state — mirrors the oscillation-related resets in
        /// AttitudeController.Start(), in the same order.</summary>
        public void Start ()
        {
            prevDetectorOmegaValid = false;
            emaOmegaValid = false;
            emaAbsHp = Vector3d.zero;
            prevControlValid = false;
            controlMeanValid = false;
            controlOscEnvelope = Vector3d.zero;
            pitchYawFreqTracker.Reset ();
            rollFreqTracker.Reset ();
            pitchYawHeldHz = double.NaN;
            rollHeldHz = double.NaN;
            pitchNotch.Reset ();
            rollNotch.Reset ();
            yawNotch.Reset ();
            for (int i = 0; i < 3; i++) {
                rateFilterValid [i] = false;
                outputFilterValid [i] = false;
                rateInputFilterValid [i] = false;
                notchActiveAxis [i] = false;
                suppressionActiveAxis [i] = false;
                suppressionRamp [i] = 0;
                mitigationLevel [i] = 0;
                oscControlBackoff [i] = 0;
                oscControlAuto [i] = 0;
                chatterLatched [i] = false;
            }
            // NOTE: chatterLevel is deliberately NOT reset — matching the live Start(), which
            // lets the level persist across re-engagements (it decays at τ = 30 s; only the
            // latch is per-engagement state).
        }

        /// <summary>
        /// The pre-target stages, in the live path's exact order: chatter detector, overrides,
        /// tracker feed, suppression selection + application, the unlatched rate-input low-pass,
        /// and the suppression ramps. Returns the suppressed rate the control loops would consume.
        /// </summary>
        public Vector3d RunPreTarget (Vector3d currentRaw, Vector3d torque, Vector3d moi, double dt,
            Services.OscillationControl pyMode, Services.OscillationControl rollMode,
            double pyManualHz, double rollManualHz, double notchQ, double detectionThreshold)
        {
            UpdateChatterDetector (currentRaw, torque, moi, dt, detectionThreshold);

            ApplyOscillationOverride (0, pyMode);
            ApplyOscillationOverride (2, pyMode);
            ApplyOscillationOverride (1, rollMode);

            if (!emaOmegaValid) {
                emaOmega = currentRaw;
                emaOmegaValid = true;
            }
            var emaOmegaBeta = 1.0 - Math.Exp (-dt / FrequencyHighpassTimeConstant);
            emaOmega = new Vector3d (
                emaOmega.x + emaOmegaBeta * (currentRaw.x - emaOmega.x),
                emaOmega.y + emaOmegaBeta * (currentRaw.y - emaOmega.y),
                emaOmega.z + emaOmegaBeta * (currentRaw.z - emaOmega.z));
            var hp = currentRaw - emaOmega;
            var betaAbsHp = 1.0 - Math.Exp (-dt / BandwidthRampTimeConstant);
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

            int pitchYawTool;
            double pitchYawFreq;
            SelectSuppressionTool (pyMode, chatterLatched [0] || chatterLatched [2],
                pitchYawHeldHz, pyManualHz, notchQ, out pitchYawTool, out pitchYawFreq);
            int rollTool;
            double rollFreq;
            SelectSuppressionTool (rollMode, chatterLatched [1],
                rollHeldHz, rollManualHz, notchQ, out rollTool, out rollFreq);
            var current = new Vector3d (
                ApplySuppression (0, currentRaw.x, pitchYawTool, pitchYawFreq, notchQ, dt, pitchNotch),
                ApplySuppression (1, currentRaw.y, rollTool, rollFreq, notchQ, dt, rollNotch),
                ApplySuppression (2, currentRaw.z, pitchYawTool, pitchYawFreq, notchQ, dt, yawNotch));

            var rateInputBeta = 1.0 - Math.Exp (-dt / DetectorRateFilterTimeConstant);
            for (int i = 0; i < 3; i++) {
                if (!rateInputFilterValid [i]) {
                    rateInputFilterState [i] = current [i];
                    rateInputFilterValid [i] = true;
                }
                rateInputFilterState [i] += rateInputBeta * (current [i] - rateInputFilterState [i]);
                var w = chatterLatched [i] ? 0.0 : chatterLevel [i];
                current [i] = current [i] + w * (rateInputFilterState [i] - current [i]);
            }

            var rampBeta = 1.0 - Math.Exp (-dt / BandwidthRampTimeConstant);
            for (int i = 0; i < 3; i++)
                suppressionRamp [i] +=
                    rampBeta * ((suppressionActiveAxis [i] ? 1.0 : 0.0) - suppressionRamp [i]);

            return current;
        }

        /// <summary>
        /// The envelope / back-off / gate stage, in the live path's exact order. Returns the
        /// per-axis gate (mitigationLevel).
        /// </summary>
        public Vector3d RunGate (double dt, double pointingError,
            Vector3d oscillationControlLevel, double oscControlThreshold)
        {
            var holdFactor = Math.Min (1.0, Math.Max (0.0,
                (HoldErrorNone - pointingError) / (HoldErrorNone - HoldErrorFull)));
            if (prevControlValid) {
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
            var pyEnv = Math.Max (controlOscEnvelope.x, controlOscEnvelope.z);
            var groupEnv = new Vector3d (pyEnv, controlOscEnvelope.y, pyEnv);
            for (int i = 0; i < 3; i++) {
                var oscTarget = chatterLatched [i] && groupEnv [i] > oscControlThreshold ? 1.0 : 0.0;
                var tc = oscTarget > oscControlAuto [i] ? ChatterRiseTimeConstant : ChatterDecayTimeConstant;
                var beta = 1.0 - Math.Exp (-dt / tc);
                oscControlAuto [i] += beta * (oscTarget - oscControlAuto [i]);
                oscControlBackoff [i] = Math.Max (oscControlAuto [i], oscillationControlLevel [i]);
            }
            var gate = new Vector3d (
                suppressionRamp [0] * Math.Max (holdFactor, oscControlBackoff [0]),
                suppressionRamp [1] * Math.Max (holdFactor, oscControlBackoff [1]),
                suppressionRamp [2] * Math.Max (holdFactor, oscControlBackoff [2]));
            mitigationLevel [0] = gate.x;
            mitigationLevel [1] = gate.y;
            mitigationLevel [2] = gate.z;
            return gate;
        }

        /// <summary>Output smoothing — verbatim copy of SmoothOutput.</summary>
        public double SmoothOutput (int index, double u, double dt)
        {
            var weight = chatterLatched [index] ? suppressionRamp [index] : chatterLevel [index];
            var tau = chatterLatched [index] ? DefaultOutputFilterTimeConstant : DetectorOutputFilterTimeConstant;
            if (!outputFilterValid [index]) {
                outputFilterState [index] = u;
                outputFilterValid [index] = true;
            }
            var beta = 1.0 - Math.Exp (-dt / tau);
            outputFilterState [index] += beta * (u - outputFilterState [index]);
            return u + weight * (outputFilterState [index] - u);
        }

        /// <summary>Store the delivered command for next tick's envelope — mirrors the live
        /// prevControl store.</summary>
        public void RecordControl (Vector3d delivered)
        {
            prevControl = delivered;
            prevControlValid = true;
        }

        void UpdateChatterDetector (Vector3d rawOmega, Vector3d torque, Vector3d moi, double dt,
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

        void ApplyOscillationOverride (int index, Services.OscillationControl mode)
        {
            if (mode == Services.OscillationControl.Off) {
                chatterLevel [index] = 0;
                chatterLatched [index] = false;
            }
        }

        void SelectSuppressionTool (Services.OscillationControl mode, bool latched, double detectedHz,
            double manualHz, double notchQ, out int tool, out double freq)
        {
            switch (mode) {
            case Services.OscillationControl.Notch:
                tool = 1;
                freq = manualHz;
                break;
            case Services.OscillationControl.LowPass:
                tool = 2;
                freq = manualHz;
                break;
            case Services.OscillationControl.Off:
                tool = 0;
                freq = 0;
                break;
            default:  // Automatic
                if (!latched) {
                    tool = 0;
                    freq = 0;
                } else if (double.IsNaN (detectedHz)) {
                    tool = 2;
                    freq = LowPassCornerMin * DefaultLowPassSeparation;
                } else {
                    freq = detectedHz;
                    tool = freq < DefaultSplitFrequency ? 1 : 2;
                }
                break;
            }
        }

        double ApplySuppression (int index, double x, int tool, double freq, double notchQ,
            double dt, BiquadNotchFilter notch)
        {
            if (tool == 1 && freq > 0) {
                rateFilterValid [index] = false;
                notchActiveAxis [index] = true;
                suppressionActiveAxis [index] = true;
                if (Math.Abs (freq - notch.ConfiguredFrequency) > 0.02 * notch.ConfiguredFrequency ||
                    notch.ConfiguredQ != notchQ || notch.ConfiguredDt != dt)
                    notch.SetFrequency (freq, notchQ, dt);
                return notch.Process (x);
            }
            if (tool == 2 && freq > 0) {
                notch.Reset ();
                notchActiveAxis [index] = false;
                suppressionActiveAxis [index] = true;
                var fc = Math.Max (LowPassCornerMin, freq / DefaultLowPassSeparation);
                var tau = 1.0 / (2.0 * Math.PI * fc);
                return LowPassAxis (index, x, tau, dt);
            }
            notch.Reset ();
            rateFilterValid [index] = false;
            notchActiveAxis [index] = false;
            suppressionActiveAxis [index] = false;
            return x;
        }

        double LowPassAxis (int index, double x, double tau, double dt)
        {
            if (!rateFilterValid [index]) {
                rateFilterStage1 [index] = x;
                rateFilterStage2 [index] = x;
                rateFilterValid [index] = true;
                return x;
            }
            var beta = 1.0 - Math.Exp (-dt / tau);
            rateFilterStage1 [index] += beta * (x - rateFilterStage1 [index]);
            rateFilterStage2 [index] += beta * (rateFilterStage1 [index] - rateFilterStage2 [index]);
            return rateFilterStage2 [index];
        }
    }
}
