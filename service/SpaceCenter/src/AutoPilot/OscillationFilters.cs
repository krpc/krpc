using System;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// A second-order (biquad) notch filter, implemented with bilinear-transform coefficients and
    /// a Direct Form II Transposed state. Used by <see cref="AttitudeController"/> to reject a
    /// low-frequency structural bending mode from the measured angular velocity while staying
    /// transparent everywhere outside its narrow rejection band — so it can sit permanently on a
    /// latched axis without smearing the sub-Hz control band the way a broadband low-pass would.
    /// </summary>
    /// <remarks>
    /// The notch has unity gain at DC and at the Nyquist frequency and zero gain at f₀; Q controls
    /// the width (higher Q is narrower: less in-band phase lag but less tolerance to frequency
    /// drift). Because the coefficient <c>K = tan(π·f₀·dt)</c> diverges as f₀ approaches Nyquist and
    /// the filter has unity gain at Nyquist by construction, a notch cannot reject a near-Nyquist
    /// mode; the controller routes those to a low-pass instead.
    /// </remarks>
    sealed class BiquadNotchFilter
    {
        double b0, b1, b2, a1, a2;
        double s1, s2;
        bool seeded;
        double configuredF;
        double configuredQ;
        double configuredDt;

        /// <summary>The frequency (Hz) the filter is currently configured for, NaN if unconfigured.</summary>
        public double ConfiguredFrequency {
            get { return configuredF; }
        }

        public double ConfiguredQ {
            get { return configuredQ; }
        }

        public double ConfiguredDt {
            get { return configuredDt; }
        }

        public bool Configured {
            get { return configuredF > 0; }
        }

        /// <summary>
        /// Compute the biquad coefficients for the given centre frequency, quality factor and
        /// sampling period. Does not touch the running state, so a live filter can be retuned to
        /// track a slowly drifting mode without a reset (only the coefficients change).
        /// </summary>
        public void SetFrequency (double f0, double Q, double dt)
        {
            var K = Math.Tan (Math.PI * f0 * dt);
            var K2 = K * K;
            var a0 = 1 + K / Q + K2;
            b0 = (1 + K2) / a0;
            b1 = 2 * (K2 - 1) / a0;
            b2 = (1 + K2) / a0;
            a1 = 2 * (K2 - 1) / a0;
            a2 = (1 - K / Q + K2) / a0;
            configuredF = f0;
            configuredQ = Q;
            configuredDt = dt;
        }

        /// <summary>
        /// Filter one sample. On the first sample after a reset the state is seeded to the
        /// steady-state for that (constant) input, so engaging on an already-rotating vessel does
        /// not produce a startup transient.
        /// </summary>
        public double Process (double x)
        {
            if (!seeded) {
                // Steady state for a constant input x (notch has unity DC gain): s1 = 0 since
                // b1 == a1, and s2 = (b2 - a2)·x.
                s1 = 0;
                s2 = (b2 - a2) * x;
                seeded = true;
            }
            var y = b0 * x + s1;
            s1 = b1 * x - a1 * y + s2;
            s2 = b2 * x - a2 * y;
            return y;
        }

        /// <summary>Clear the running state (the next <see cref="Process"/> re-seeds).</summary>
        public void Reset ()
        {
            s1 = 0;
            s2 = 0;
            seeded = false;
        }
    }

    /// <summary>
    /// Online estimator of a structural oscillation frequency, tracked from the interval between
    /// sign changes of the tick-to-tick rate change (Δω). One instance per axis group (pitch/yaw,
    /// roll). It is fed every physics tick regardless of whether suppression is engaged — it costs
    /// only a handful of adds — so an estimate is always warm the moment an axis is deemed flexible.
    /// </summary>
    /// <remarks>
    /// Δω changes sign at each extremum of ω — twice per oscillation cycle — so the interval between
    /// accepted sign flips is a *half* period and the full period is twice that. A hysteresis
    /// deadband on the zero crossing rejects jitter around an extremum; a plausibility gate rejects
    /// slew transients (too slow) and single-tick noise (too fast); and the estimate stays NaN until
    /// several consecutive half-periods agree, and reverts to NaN if the mode dies.
    /// </remarks>
    sealed class FrequencyTracker
    {
        // Deadband as a fraction of the smoothed |Δω| (κ): a crossing must exceed this to count.
        const double DeadbandFactor = 0.25;
        // EMA time constant for the smoothed |Δω| that sets the deadband, and for the period.
        const double AbsDeltaTimeConstant = 0.5;
        const double PeriodTimeConstant = 0.5;
        // Plausibility gate (Hz). f_min rejects slew transients (too slow). The upper bound is
        // Nyquist (0.5/dt, = 25 Hz at 50 Hz physics), computed per tick from dt: a near-Nyquist
        // structural mode (e.g. the every-other-tick ~25 Hz chatter of a craft with tip-mounted
        // actuators) alternates Δω every tick, giving a one-tick half-period at exactly Nyquist, so
        // the cap must include it — it is then acquired and routed to the low-pass branch. Single-
        // tick noise also reads at Nyquist but is rejected by the AcquireCount agreement gate.
        const double MinFrequency = 0.5;
        // Consecutive accepted half-periods that must agree (within AgreeTolerance) before the
        // estimate is published, and the window with no accepted crossing after which it is lost.
        const int AcquireCount = 3;
        const double AgreeTolerance = 0.2;
        const int LossTicks = 8;

        double emaAbsDelta;
        int signState;
        int ticksSinceFlip;
        int ticksSinceAccepted;
        double periodEma;
        bool periodEmaValid;
        int agreeCount;
        double estimatedHz = double.NaN;

        /// <summary>The estimated oscillation frequency in Hz, or NaN until the estimator acquires.</summary>
        public double EstimatedHz {
            get { return estimatedHz; }
        }

        /// <summary>
        /// Number of consecutive accepted half-periods that agree so far (resets on
        /// disagreement or loss). The estimate publishes at <c>AcquireCount</c>; exposed for
        /// the diagnostic log so acquisition progress is visible.
        /// </summary>
        public int AgreeCount {
            get { return agreeCount; }
        }

        public void Reset ()
        {
            emaAbsDelta = 0;
            signState = 0;
            ticksSinceFlip = 0;
            ticksSinceAccepted = 0;
            periodEma = 0;
            periodEmaValid = false;
            agreeCount = 0;
            estimatedHz = double.NaN;
        }

        /// <summary>
        /// Feed one tick's Δω (the raw, pre-suppression rate change on the tracked axis).
        /// </summary>
        public void Update (double delta, double dt)
        {
            var betaAbs = 1.0 - Math.Exp (-dt / AbsDeltaTimeConstant);
            emaAbsDelta += betaAbs * (Math.Abs (delta) - emaAbsDelta);

            ticksSinceFlip++;
            ticksSinceAccepted++;

            var deadband = DeadbandFactor * emaAbsDelta;
            var sign = delta > deadband ? 1 : delta < -deadband ? -1 : 0;

            if (sign != 0 && signState == 0) {
                // First sign acquisition: start the half-period clock, nothing to measure yet.
                signState = sign;
                ticksSinceFlip = 0;
            } else if (sign != 0 && sign != signState) {
                // Accepted zero crossing: the ticks since the previous flip are a half-period.
                var period = 2.0 * ticksSinceFlip * dt;
                var f = period > 0 ? 1.0 / period : 0.0;
                var nyquist = dt > 0 ? 0.5 / dt : double.MaxValue;
                if (f >= MinFrequency && f <= nyquist * (1.0 + 1e-9)) {
                    if (periodEmaValid) {
                        var fEma = 1.0 / periodEma;
                        if (Math.Abs (f - fEma) <= AgreeTolerance * fEma)
                            agreeCount++;
                        else
                            agreeCount = 1;
                        var betaPeriod = 1.0 - Math.Exp (-period / PeriodTimeConstant);
                        periodEma += betaPeriod * (period - periodEma);
                    } else {
                        periodEma = period;
                        periodEmaValid = true;
                        agreeCount = 1;
                    }
                    if (agreeCount >= AcquireCount)
                        estimatedHz = 1.0 / periodEma;
                    ticksSinceAccepted = 0;
                }
                ticksSinceFlip = 0;
                signState = sign;
            }

            // Mode died / craft settled: drop the estimate so a stale frequency is not used.
            if (ticksSinceAccepted > LossTicks) {
                estimatedHz = double.NaN;
                agreeCount = 0;
                periodEmaValid = false;
            }
        }
    }
}
