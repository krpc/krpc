using System;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// The rate-feedback mitigation primitive: per-axis filtering of the measured angular
    /// velocity. Two independent stages, both owned here so a regime change never injects a
    /// state transient:
    /// a heavy stage — notch(f, Q) or two cascaded first-order low-passes or pass-through,
    /// selected per tick by the mitigation policy (the latched flexible-craft suppression) —
    /// and a light one-pole stage blended in by a [0,1] weight (the detector-firing-but-
    /// unlatched measured-rate low-pass). Which stage runs, at what configuration and weight,
    /// is entirely the policy's decision; this class only owns the filter state and arithmetic.
    /// </summary>
    sealed class RateFilter
    {
        // Time constant of the light input stage (~5 Hz corner): stops the full-bandwidth inner
        // loop (and the stopping term) chasing root-part rate jitter on an unlatched noisy
        // craft. It sits inside the loop, so its phase counts: ~16° at the ~1.4 Hz crossover.
        internal const double DetectorInputTimeConstant = 0.03;

        // Heavy stage: one notch per axis and the 2-stage low-pass state. The low-pass state is
        // seeded with the first sample (no startup transient) and held reset while inactive.
        readonly BiquadNotchFilter[] notches = {
            new BiquadNotchFilter (), new BiquadNotchFilter (), new BiquadNotchFilter ()
        };
        readonly double[] stage1 = new double[3];
        readonly double[] stage2 = new double[3];
        readonly bool[] lowPassValid = new bool[3];
        // Light stage: always-updating one-pole per axis, blended by the caller's weight.
        readonly double[] inputFilterState = new double[3];
        readonly bool[] inputFilterValid = new bool[3];
        // Records of the heavy stage's last decision (read by the policy's ramps and the
        // diagnostics glyphs).
        readonly bool[] notchActive = new bool[3];
        readonly bool[] suppressionActive = new bool[3];

        public bool NotchActive (int index)
        {
            return notchActive [index];
        }

        public bool LowPassActive (int index)
        {
            return lowPassValid [index];
        }

        public bool SuppressionActive (int index)
        {
            return suppressionActive [index];
        }

        public void Reset ()
        {
            for (int i = 0; i < 3; i++) {
                notches [i].Reset ();
                lowPassValid [i] = false;
                inputFilterValid [i] = false;
                notchActive [i] = false;
                suppressionActive [i] = false;
            }
        }

        /// <summary>
        /// Apply the heavy suppression stage to one axis: <paramref name="tool"/> 0 = pass
        /// through, 1 = notch at (freq, q), 2 = two cascaded first-order low-passes with corner
        /// max(<paramref name="lowPassCornerMin"/>, freq / <paramref name="lowPassSeparation"/>).
        /// The inactive filter's state is held reset so a regime change injects no transient.
        /// The notch is reconfigured only when the frequency has moved materially (&gt;2%), Q
        /// changed, or the sampling period changed, so a slowly drifting mode is tracked without
        /// thrashing.
        /// </summary>
        public double Apply (int index, double x, int tool, double freq, double q,
            double lowPassSeparation, double lowPassCornerMin, double dt)
        {
            var notch = notches [index];
            if (tool == 1 && freq > 0) {
                lowPassValid [index] = false;
                notchActive [index] = true;
                suppressionActive [index] = true;
                if (Math.Abs (freq - notch.ConfiguredFrequency) > 0.02 * notch.ConfiguredFrequency ||
                    notch.ConfiguredQ != q || notch.ConfiguredDt != dt)
                    notch.SetFrequency (freq, q, dt);
                return notch.Process (x);
            }
            if (tool == 2 && freq > 0) {
                notch.Reset ();
                notchActive [index] = false;
                suppressionActive [index] = true;
                var fc = Math.Max (lowPassCornerMin, freq / lowPassSeparation);
                var tau = 1.0 / (2.0 * Math.PI * fc);
                return LowPassAxis (index, x, tau, dt);
            }
            notch.Reset ();
            lowPassValid [index] = false;
            notchActive [index] = false;
            suppressionActive [index] = false;
            return x;
        }

        /// <summary>
        /// Light input stage: update the always-running one-pole and blend it in by
        /// <paramref name="weight"/> (0 = untouched). The state is seeded with the first sample.
        /// </summary>
        public double BlendInput (int index, double x, double weight, double dt)
        {
            if (!inputFilterValid [index]) {
                inputFilterState [index] = x;
                inputFilterValid [index] = true;
            }
            var beta = 1.0 - Math.Exp (-dt / DetectorInputTimeConstant);
            inputFilterState [index] += beta * (x - inputFilterState [index]);
            return x + weight * (inputFilterState [index] - x);
        }

        double LowPassAxis (int index, double x, double tau, double dt)
        {
            if (!lowPassValid [index]) {
                stage1 [index] = x;
                stage2 [index] = x;
                lowPassValid [index] = true;
                return x;
            }
            var beta = 1.0 - Math.Exp (-dt / tau);
            stage1 [index] += beta * (x - stage1 [index]);
            stage2 [index] += beta * (stage1 [index] - stage2 [index]);
            return stage2 [index];
        }
    }

    /// <summary>
    /// The output-smoothing mitigation primitive: a per-axis first-order low-pass on the final
    /// actuator command, blended in by a [0,1] weight. Caps residual control chatter directly —
    /// gain-independent and needing no frequency estimate — backstopping the rate filtering,
    /// which cleans the feedback but not whatever the loop's own gain still produces at the
    /// mode. The corner and weight are the policy's per-tick decision (latched axes use the
    /// heavier corner at the suppression-ramp weight; detector-firing unlatched axes the lighter
    /// corner at the chatter-level weight); this class only owns the filter state.
    /// </summary>
    sealed class OutputFilter
    {
        // Corner for a latched axis (~2 Hz): attenuates residual chatter with negligible phase
        // cost at the floored (~0.16 Hz) crossover.
        internal const double LatchedTimeConstant = 0.08;
        // Corner for a detector-firing but unlatched axis (~4.5 Hz): the loop still runs at full
        // bandwidth there, so the latched corner would cost ~36° of phase margin and risk
        // oscillation; ~4.5 Hz costs only ~16° while attenuating near-Nyquist buzz ~5×.
        internal const double DetectorTimeConstant = 0.035;

        readonly double[] state = new double[3];
        readonly bool[] valid = new bool[3];

        public void Reset ()
        {
            for (int i = 0; i < 3; i++)
                valid [i] = false;
        }

        /// <summary>
        /// Update the one-pole (seeded with the first sample) and blend it in by
        /// <paramref name="weight"/> (0 = pass through).
        /// </summary>
        public double Process (int index, double u, double tau, double weight, double dt)
        {
            if (!valid [index]) {
                state [index] = u;
                valid [index] = true;
            }
            var beta = 1.0 - Math.Exp (-dt / tau);
            state [index] += beta * (u - state [index]);
            return u + weight * (state [index] - u);
        }
    }
}
