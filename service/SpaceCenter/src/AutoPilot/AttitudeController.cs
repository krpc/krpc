using System;
using System.Text;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// Controller to hold a vessels attitude in a chosen orientation.
    /// </summary>
    sealed class AttitudeController
    {
        readonly Services.Vessel vessel;
        public readonly PIDController PitchPID = new PIDController ();
        public readonly PIDController RollPID = new PIDController ();
        public readonly PIDController YawPID = new PIDController ();

        // Target orientation — quaternion is the single source of truth.
        // rollControlled=false means "suppress roll rotation" (don't hold a specific angle).
        // targetRotation is the COMMANDED target (what the user set); the public getters and
        // AutoPilot.Error read it. The control loop instead consumes effectiveRotation, which is
        // slewed toward the commanded target over TargetSmoothingTime seconds (see Update). When
        // smoothing is disabled (the default), effectiveRotation == targetRotation every tick.
        QuaternionD targetRotation;
        bool rollControlled;
        // Target smoothing (slew): effectiveRotation is the slewed target the control loop tracks.
        // Each tick it RotateTowards the commanded targetRotation at slewSpeed (deg/s). slewSpeed is
        // latched whenever the target changes to (angle between effective and commanded) /
        // targetSmoothingTime, so an isolated change arrives in exactly targetSmoothingTime seconds
        // (constant angular rate), while a continuous stream of changes re-latches each tick and
        // settles into a smooth, bounded lag (~targetSmoothingTime x change-rate) with no freeze at
        // any update cadence. 0 = instant (no slew). slewPending is set by the (RPC-thread) setters
        // and consumed in Update, on the physics thread.
        QuaternionD effectiveRotation;
        double targetSmoothingTime;
        double slewSpeed;
        bool slewPending;

        // PID autotuning variables
        Vector3d overshoot;
        Vector3d timeToPeak;
        Vector3d twiceZetaOmega = Vector3d.zero;
        Vector3d omegaSquared = Vector3d.zero;
        // One-sided smoothed torque: tracks increases immediately, decays decreases at τ≈0.5s.
        // Passed to DoAutoTune so a sudden drop (e.g. engine shutdown while a reaction wheel
        // remains) does not cause a one-tick gain spike that jerks the gimbal.
        Vector3d smoothedTorque;
        // Previous tick's target angular velocity in the roll-invariant frame, used to
        // compute the acceleration feedforward. prevTargetRiValid is false on the first
        // tick after Start() so the derivative is not taken across a discontinuity.
        Vector3d prevTargetRi;
        bool prevTargetRiValid;
        // Low-pass-filtered acceleration feedforward (see the feedforward filter in Update).
        Vector3d smoothedFfRi;
        // Per-axis state of the two cascaded first-order sections of the angular-velocity low-pass
        // (see LowPassAxis), used as the high-frequency suppression tool. rateFilterValid is false
        // until the section is first used (or after it is reset when the axis is not low-passed), so
        // the state is seeded with the raw measurement rather than ramping up from zero.
        readonly double[] rateFilterStage1 = new double[3];
        readonly double[] rateFilterStage2 = new double[3];
        readonly bool[] rateFilterValid = new bool[3];
        // Per-axis state of the output low-pass: a first-order filter on the final actuator command,
        // blended in by suppressionRamp on a latched axis (MechJeb-style output smoothing). It caps
        // residual control chatter directly — gain-independent and needing no frequency estimate —
        // backstopping the notch/low-pass, which clean the feedback but not whatever the loop's own
        // gain still produces at the mode. Negligible phase cost at the floored crossover.
        readonly double[] outputFilterState = new double[3];
        readonly bool[] outputFilterValid = new bool[3];
        // Per-axis state of the measured-rate low-pass applied to a detector-firing but *unlatched* axis
        // (the noisy-but-controllable craft). Distinct from the suppression filters above, which only run
        // on a latched axis; this light filter blended by chatterLevel stops the full-bandwidth inner loop
        // (and the ω/bandwidth stopping term) chasing the root-part rate jitter on an unlatched craft.
        readonly double[] rateInputFilterState = new double[3];
        readonly bool[] rateInputFilterValid = new bool[3];
        // Three notch filters (the low-frequency suppression tool), one per axis: pitch (0) and
        // yaw (2) configured from the pitch/yaw tracker, roll (1) from the roll tracker.
        readonly BiquadNotchFilter pitchNotch = new BiquadNotchFilter ();
        readonly BiquadNotchFilter rollNotch = new BiquadNotchFilter ();
        readonly BiquadNotchFilter yawNotch = new BiquadNotchFilter ();
        // Online frequency estimators, fed the high-passed rate every tick: one for the coupled
        // pitch/yaw group (fed whichever transverse axis currently carries more oscillation) and one
        // for roll.
        readonly FrequencyTracker pitchYawFreqTracker = new FrequencyTracker ();
        readonly FrequencyTracker rollFreqTracker = new FrequencyTracker ();
        // Sticky held estimate per group: latched to the last value the tracker acquired and held
        // across brief losses (NaN only until the first acquisition, reset in Start). Suppression and
        // the bandwidth reduction *quiet* the very mode the tracker measures, so the live estimate
        // would otherwise blip to NaN once it works and revert routing to the seed — a notch/low-pass
        // hunt. Routing and the *OscillationDetectedFrequency observable both use this held value.
        double pitchYawHeldHz = double.NaN;
        double rollHeldHz = double.NaN;
        // High-pass bookkeeping for the trackers. The trackers are fed the oscillation in the raw
        // rate — ω minus its slow mean (emaOmega) — rather than Δω: Δω is a derivative, so it weights
        // high frequencies (a small high-frequency component can dominate the Δω sign changes and pull
        // the estimate above the mode that actually dominates the rate). High-passed ω instead tracks
        // the dominant-amplitude oscillation. emaAbsHp is a slow per-axis envelope of |high-passed ω|
        // used to pick the stronger transverse axis for the pitch/yaw group estimate.
        Vector3d emaOmega;
        bool emaOmegaValid;
        Vector3d emaAbsHp = Vector3d.zero;
        // Per-axis records written during suppression selection: whether the notch is the active tool
        // (diagnostics) and whether any suppression tool is active (read by DoAutoTuneAxis to gate the
        // latched bandwidth reduction).
        readonly bool[] notchActiveAxis = new bool[3];
        readonly bool[] suppressionActiveAxis = new bool[3];
        // Per-axis one-pole ramp [0,1] easing the latched output smoothing in/out so the control does
        // not step when suppression engages/releases (reset in Start).
        readonly double[] suppressionRamp = new double[3];
        // Per-axis hold-gated mitigation weight (suppressionRamp · holdFactor) written each tick and
        // read by DoAutoTuneAxis: the bandwidth floor follows this, not suppressionRamp, so a latched
        // axis runs at full bandwidth while slewing (responsive) and is floored only while holding.
        readonly double[] mitigationLevel = new double[3];
        // Per-axis [0,1] oscillation-control back-off OR'd into the hold gate (gate = suppressionRamp ·
        // max(holdFactor, oscControlBackoff)), so the latched flexible-craft mitigation (bandwidth floor
        // + feedforward cut + nominal target) can be engaged regardless of pointing error — decoupling
        // it from the hold gate, which otherwise releases the mitigation during a maneuver and lets a
        // limit cycle build. Recomputed each tick from oscillationControlLevel; reset in Start.
        readonly double[] oscControlBackoff = new double[3];
        // Manual per-axis (pitch, roll, yaw) override level for oscControlBackoff, set from the public
        // API (OscillationControlLevel). 0 by default (no override, behaviour byte-identical to before).
        // Config, so it persists across re-engage (set in Reset, not cleared in Start).
        Vector3d oscillationControlLevel = Vector3d.zero;
        // Automatic (envelope-driven) component of oscControlBackoff before the manual floor is applied:
        // the per-axis ramp that rises while a latched axis limit-cycles and decays slowly when quiet.
        readonly double[] oscControlAuto = new double[3];
        // Previous tick's delivered command (state.Pitch/Roll/Yaw), and the slow trim mean plus the
        // about-mean envelope built from it one tick late (the gate that consumes the back-off is
        // computed before this tick's command exists). controlOscEnvelope is the runtime analogue of
        // the test's control_oscillation_amplitude.
        Vector3d prevControl;
        bool prevControlValid;
        Vector3d controlMean;
        bool controlMeanValid;
        Vector3d controlOscEnvelope = Vector3d.zero;
        // Envelope threshold (exposed) above which a latched axis is treated as still limit-cycling.
        double oscControlThreshold;
        // Chatter-detector state for the latch (see UpdateChatterDetector). chatterLevel is a
        // per-axis [0,1] measure of how strongly an axis is in a structural limit cycle.
        Vector3d prevDetectorOmega;
        bool prevDetectorOmegaValid;
        Vector3d chatterLevel = Vector3d.zero;
        // Per-axis latch: set once an axis's chatterLevel crosses ChatterLatchThreshold, i.e. the
        // detector has confirmed a structural limit cycle. It is the controller's persistent memory
        // that "this craft is flexible", reset only on re-engage (Start). In Automatic mode it gates
        // whether suppression is applied at all (the estimator, which selects the tool, runs ungated).
        readonly bool[] chatterLatched = new bool[3];
        // Per-group suppression selector (pitch/yaw coupled, roll on its own). Automatic detects and
        // latches then routes by estimated frequency; Off disables suppression; Notch/LowPass force
        // that tool unconditionally at the group's manual frequency. Default Automatic.
        Services.OscillationControl pitchYawOscillationControl;
        Services.OscillationControl rollOscillationControl;
        // Per-group mode frequency (Hz): used directly in Notch/LowPass mode and as the Automatic
        // estimator seed before acquisition.
        double pitchYawOscillationFrequency;
        double rollOscillationFrequency;
        // Notch quality factor and the notch-branch bandwidth separation ratio N (bw_target =
        // 2π·f/N). Exposed so an advanced user can trade phase margin against drift tolerance.
        double oscillationNotchQ;
        double oscillationBandwidthFloor;
        double oscillationDetectionThreshold;
        // Engagement soft-start: the actuator command is faded in over SoftStartTime seconds from
        // each Start() so engagement does not deliver a near-max "kick" (large proportional +
        // acceleration feedforward) that can impulsively excite a structural or in-band limit cycle
        // — the transient the manual "settle before liftoff" pause avoids. engageFixedTime is the
        // Time.fixedTime at the last Start(); the ramp is a function of elapsed time from it. While
        // the craft is held on the launch clamps (PRELAUNCH) Update re-pins it each tick, so the fade
        // begins at clamp release rather than at engagement.
        double softStartTime;
        double engageFixedTime;
        // Continuity state for the roll-invariant frame. The pointing-only rotation (AP-frame up ->
        // nose) used to be re-derived each tick as FromToRotation(up, nose), but that is singular when
        // the nose passes through -up (e.g. due south on the horizon in the surface frame, where the
        // y-axis is north): the minimal-arc rotation's axis is hypersensitive to transverse motion
        // there, so a tiny control jitter whips the RI frame ~180 deg over a vanishing arc and the
        // stateful feedforward/integral turn that into a full-deflection kick. Instead the rotation is
        // carried forward by the well-conditioned minimal rotation between consecutive nose directions
        // (the nose cannot reverse in one physics tick), so the frame never sees the antipode
        // singularity. Seeded once per engage from the fixed reference; reset in Start.
        Vector3d prevPointDirection;
        QuaternionD pointRotation;
        bool pointRotationValid;
        // Antipodal-flip plane latch (see ResolveAntipodalAxis). When the error enters the antipode
        // band with enough rotation to define a flip plane, the plane normal is captured once here
        // and held for the rest of the pass; letting it track the live angular velocity instead
        // feeds any out-of-plane drift back on itself and the flip tumbles. Cleared on leaving the
        // band and on re-engage (Start).
        Vector3d antipodeLatchedNormal;
        bool antipodeLatched;
        Vector3d logAngles;
        // Raw (unfiltered) angular velocity in the roll-invariant frame, recorded for diagnostics
        // so the filter's effect on a flexible craft is visible against what the loop acts on.
        Vector3d logRawOmegaRi;
        // Target angular velocity (roll-invariant frame) the inner loop is tracking this tick,
        // recorded for the info UI alongside the measured rate.
        Vector3d logTargetRi;
        bool diagnosticLogging;
        readonly StringBuilder diagnosticLog = new StringBuilder ();
        readonly object diagnosticLogLock = new object ();

        // Time constant for the one-sided torque smoothing (see UpdateSmoothedTorque).
        const double TorqueSmoothTimeConstant = 0.5;
        // Time constant for the acceleration-feedforward low-pass filter (a few physics ticks).
        // Long enough to attenuate the single-tick steps the bang-bang profile's slope
        // discontinuities produce, short enough that the feedforward lag stays negligible.
        const double FeedforwardSmoothTimeConstant = 0.05;
        // Default per-group mode frequency (Hz): the manual notch/low-pass frequency and the
        // Automatic-mode estimator seed before acquisition. 1.5 Hz is near the only in-game-measured
        // low-frequency mode (the Ariane 5's ~1.4 Hz first lateral bending mode), so a freshly
        // latched low-frequency axis routes to the notch branch and is never momentarily un-suppressed.
        const double DefaultOscillationFrequency = 1.5;
        // Default notch quality factor: a higher Q is a narrower notch (less in-band phase lag but
        // less tolerance to frequency drift); a lower Q is wider.
        const double DefaultNotchQ = 2.5;
        // Default inner-loop bandwidth floor (rad/s) a latched axis is reduced toward. This is the
        // primary, frequency-independent gain-stabiliser: dropping the rate-loop crossover well below
        // every structural mode (the stock flexible craft sit at ~1.4–25 Hz) stops the loop driving
        // any of them, robustly and without needing an accurate mode-frequency estimate. The notch /
        // low-pass clean the rate feedback on top; output smoothing caps any residual. Only ever
        // lowers bandwidth (min against the autotuned value), so rigid craft — which never latch —
        // are untouched. ~1.0 rad/s matches the value validated on the earlier adaptive design.
        const double DefaultBandwidthFloor = 1.0;
        // Time constant of the latched output low-pass (~2 Hz corner): attenuates residual control
        // chatter while adding negligible phase at the floored crossover (~0.16 Hz).
        const double DefaultOutputFilterTimeConstant = 0.08;
        // Time constant of the output low-pass for a detector-firing but *unlatched* axis (~4.5 Hz
        // corner). This is the noisy-but-controllable craft: the Δω detector fires on root-part rate
        // jitter but the level has not (yet) crossed the latch threshold, so the full mitigation never
        // engages and the rate loop runs at full bandwidth (~1.4 Hz crossover). At full bandwidth the
        // aggressive autotuned gain turns
        // that jitter into near-full-scale tick-to-tick actuator reversals; smoothing the delivered
        // command tames it. The corner is lighter than the latched 2 Hz (which would cost ~36° of phase
        // margin here and risk oscillation) — ~4.5 Hz costs only ~16° while still attenuating the
        // near-Nyquist buzz ~5×. Blended by chatterLevel, so rigid craft (level ~0) are untouched.
        const double DetectorOutputFilterTimeConstant = 0.035;
        // Time constant of the *measured-rate* low-pass for a detector-firing but unlatched axis (~5 Hz
        // corner). The companion to the output smoothing above, on the loop input: it stops the inner rate
        // loop (and the stopping-distance term ω/bandwidth that feeds the acceleration feedforward) chasing
        // the root-part rate jitter in the first place — the actual source of the buzz, which shaping the
        // setpoint or the output alone cannot remove. It sits inside the loop, so its phase counts against
        // stability; ~5 Hz costs ~16° at the ~1.4 Hz crossover. Lowering it to ~3.5 Hz was tried and made
        // no measurable difference to the residual (which is lower-frequency content that passes either
        // corner), so the higher corner is kept for the extra margin. Blended by chatterLevel so rigid
        // craft are untouched, and skipped on a latched axis (the heavier suppression runs there).
        const double DetectorRateFilterTimeConstant = 0.03;
        // Pointing-error band (degrees) for the continuous hold gate on a latched axis: at/below
        // HoldErrorFull the craft is holding and the loop fully tracks the rate-independent nominal
        // target with the feedforward cut (so a residual mode is not amplified at the floored gain);
        // at/above HoldErrorNone it is slewing and uses the full target + feedforward (so it brakes
        // and tracks the manoeuvre); it blends linearly between. A smooth function of error — no
        // hysteresis state machine. The band sits just above the stopping threshold (1°) so the
        // mitigation engages only once the craft is essentially settled: a slew or a nudge that
        // leaves the error above ~2.5° keeps full bandwidth (responsive), and the final approach is
        // not slowed by the floor — only the settled hold is quieted.
        const double HoldErrorFull = 1.0;
        const double HoldErrorNone = 2.5;
        // Pointing deadband on the target angular velocity (see DeadbandScale, applied in
        // ComputePitchYawVelocity / ComputeAxisVelocity). As the error vanishes the commanded velocity
        // setpoint is scaled linearly to zero — from full at the deadband high angle to zero below this
        // fraction of it — so the craft coasts to a stop inside the band and the inner rate loop is no
        // longer commanded to chase sub-band motion, which on a craft with a noisy root-part rate would
        // dither the actuators. It is applied to the *setpoint* (outer loop), not the inner-loop gains, so
        // the inner rate loop keeps its full proportional damping and stays well-damped at the hold point
        // (scaling the inner proportional term instead removes that damping and the hold oscillates). This
        // replaces the former logistic attenuation with a linear ramp that reaches exactly zero — a clean
        // deadband rather than a residual tail. The high angle is the public PitchYawAttenuationAngle /
        // RollAttenuationAngle (formerly the logistic half-width); the low edge is this fraction of it.
        const double DeadbandLowFraction = 0.5;
        // Routing threshold (Hz): a detected mode below this is well-conditioned for a notch (K =
        // tan(π·f·dt) < 1 up to 12.5 Hz at 50 Hz physics) and close enough to the band that a
        // low-pass would add too much crossover phase lag; above it the notch degrades toward the
        // Nyquist singularity and the low-pass is the right tool. Set to 12 Hz on measured data: the
        // stock flexible test craft sit at ~2.5–8.5 Hz (notch) with only near-Nyquist roll modes
        // (~21–25 Hz) on the low-pass.
        const double DefaultSplitFrequency = 12.0;
        // Low-pass corner separation: the high-frequency-branch corner is f_detected / L_lp, clamped
        // to LowPassCornerMin (Hz) so it never drifts down into the sub-Hz control band.
        const double DefaultLowPassSeparation = 3.0;
        const double LowPassCornerMin = 2.0;
        // Time constant of the one-pole ramp easing the notch-branch bandwidth reduction in/out.
        const double BandwidthRampTimeConstant = 0.5;
        // Time constant of the slow mean subtracted from the raw rate to high-pass it for the
        // frequency trackers (~0.5 Hz corner): well below the structural modes (≥1 Hz) so they pass,
        // but high enough to remove DC and slew trends so they do not manufacture false crossings.
        const double FrequencyHighpassTimeConstant = 0.3;
        // A tick-to-tick change in the raw measured rate larger than this many times the
        // physically-achievable change (α·dt at full authority) is structural excitation, not
        // rigid-body response — the latter cannot change the rate faster than the torque allows.
        const double DefaultChatterDetectThreshold = 4.0;
        // One-sided smoothing time constants for the detector: engage quickly when excitation
        // appears, release slowly so the loop does not hunt (the reduced-bandwidth state is quiet,
        // which would otherwise immediately clear the detector and let the bandwidth climb back up).
        const double ChatterRiseTimeConstant = 0.3;
        const double ChatterDecayTimeConstant = 30.0;
        // chatterLevel above which an axis is latched into full mitigation for the rest of the
        // engagement (see chatterLatched). Set below the level the detector reaches on a confirmed
        // limit cycle (which saturates toward 1) but well above any transient a rigid craft produces,
        // so a rigid craft — whose chatterLevel never leaves ~0 — is never latched.
        const double ChatterLatchThreshold = 0.6;
        // Control-output oscillation envelope (the runtime analogue of the test's
        // control_oscillation_amplitude): a latched axis whose about-mean delivered-command envelope
        // exceeds this is treated as still limit-cycling, so the hold mitigation engages regardless of
        // pointing error. A settled hold sits near 0.008 and a limit cycle saturates toward ~1, so the
        // 0.2 default has wide margin. Exposed (OscillationControlThreshold).
        const double DefaultOscControlThreshold = 0.2;
        // Time constant of the slow "trim" mean subtracted from the delivered command to form the
        // about-mean envelope: long enough to track a steady slew (so a one-sign ramp is not counted as
        // oscillation) yet shorter than the limit-cycle period (~0.7 s), which is left as deviation.
        const double OscControlMeanTimeConstant = 0.5;
        // Time constant smoothing the |command - trim| envelope (a few cycles).
        const double OscControlEnvelopeTimeConstant = 0.3;
        // Below this 2D error magnitude (radians) the joint pitch/yaw profile is skipped.
        const double MinThetaForJointProfile = 1e-10;
        // Antipodal-flip plane hold (ResolveAntipodalAxis). A large slew toward ~180° (an "antipodal
        // flip") must ride a single committed plane, but the live geodesic axis
        // FromToRotation(current, target) precesses fastest exactly when current and target are
        // near-antipodal — and the vessel unavoidably crawls through that region while the rate loop
        // ramps the angular velocity up against inertia. Chasing the precessing axis there pumps the
        // nose out of plane; the slower the traverse the worse it is, so a from-rest or lightly-seeded
        // flip (which crawls longest) bows the most, while a fast 90° slew — never near-antipodal — is
        // untouched. So within AntipodeBlendAngle of antipodal the commanded axis is held to a plane
        // *latched once* on entering the band, rather than tracking the precessing live axis: fully
        // (weight 1) within AntipodeHoldAngle of antipodal — covering the crawl/pump region — then
        // smoothstep-blended back to the live geodesic axis between AntipodeHoldAngle and
        // AntipodeBlendAngle so the hand-back is continuous and ordinary (< AntipodeBlendAngle) slews
        // are untouched. The latched plane is a great circle through the current nose and its
        // antipode = the target, so holding it still carries the nose to the target.
        const double AntipodeBlendAngle = 50.0;
        const double AntipodeHoldAngle = 35.0;
        // Perpendicular-rate threshold (rad/s) selecting where the latched plane comes from
        // (ResolveAntipodalAxis): above it the vessel is already committed to a rotation, so latch its
        // plane (the cleanest definition of where the flip is going); below it — a from-rest flip —
        // latch the arbitrary-but-consistent geodesic axis instead. Either way a plane is latched and
        // held, so even a from-rest flip rides a fixed plane rather than the precessing live axis. Set
        // well below a real flip's rate but above a settled craft's residual sensor/physics noise
        // (~1e-4), so a light deliberate seed (~1e-2) still latches its own plane.
        const double AntipodeLeadRate = 0.004;
        // Default engagement soft-start duration (seconds): the actuator command is faded in over
        // this window from each engage so the engagement transient cannot kick a fresh, full-authority
        // gimbal into a limit cycle. ~0.5 s is short enough to be imperceptible on a settled craft yet
        // long enough to spread the engagement step over many physics ticks. Set to 0 to disable.
        const double DefaultSoftStartTime = 0.5;

        public AttitudeController (Vessel vessel)
        {
            this.vessel = new Services.Vessel (vessel);
            Reset ();
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public double TargetPitch {
            get { return targetRotation.PitchHeadingRoll ().x; }
            set {
                var p = targetRotation.PitchHeadingRoll ();
                SetTarget (value, p.y, rollControlled ? p.z : double.NaN);
            }
        }

        public double TargetHeading {
            get { return targetRotation.PitchHeadingRoll ().y; }
            set {
                var p = targetRotation.PitchHeadingRoll ();
                SetTarget (p.x, value, rollControlled ? p.z : double.NaN);
            }
        }

        public double TargetRoll {
            get { return rollControlled ? targetRotation.PitchHeadingRoll ().z : double.NaN; }
            set {
                var p = targetRotation.PitchHeadingRoll ();
                SetTarget (p.x, p.y, value);
            }
        }

        public Vector3d TargetDirection {
            get { return targetRotation * Vector3d.up; }
        }

        public QuaternionD TargetRotation {
            get { return targetRotation; }
        }

        // The effective (slewed) target — what the control loop is currently tracking. Equal to the
        // commanded target above when smoothing is off; lags it during a slew. rollControlled is
        // shared with the commanded target (a single mode flag), so the roll readouts match.
        public double EffectiveTargetPitch {
            get { return effectiveRotation.PitchHeadingRoll ().x; }
        }

        public double EffectiveTargetHeading {
            get { return effectiveRotation.PitchHeadingRoll ().y; }
        }

        public double EffectiveTargetRoll {
            get { return rollControlled ? effectiveRotation.PitchHeadingRoll ().z : double.NaN; }
        }

        public Vector3d EffectiveTargetDirection {
            get { return effectiveRotation * Vector3d.up; }
        }

        public QuaternionD EffectiveTargetRotation {
            get { return effectiveRotation; }
        }

        public double TargetSmoothingTime {
            get { return targetSmoothingTime; }
            set { targetSmoothingTime = Math.Max (0, value); }
        }

        // Request the slew speed be (re)latched from the current effective-to-commanded gap. Called
        // after every target change. The latch happens in Update (the physics thread); the setters
        // may run on the RPC thread.
        void BeginSlew ()
        {
            slewPending = true;
        }

        void SetTarget (double pitch, double heading, double roll)
        {
            rollControlled = !double.IsNaN (roll);
            var phr = new Vector3d (pitch, heading, rollControlled ? roll : 0);
            targetRotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (phr);
            BeginSlew ();
        }

        public void SetTargetDirection (Vector3d direction)
        {
            targetRotation = GeometryExtensions.FromToRotation (Vector3d.up, direction.normalized);
            rollControlled = false;
            BeginSlew ();
        }

        public void SetTargetRotation (QuaternionD rotation)
        {
            targetRotation = rotation;
            rollControlled = true;
            BeginSlew ();
        }

        public Vector3d MaxAngularVelocity { get; set; }

        // Pointing deadband high angle (degrees) for the roll axis: at/above this error the target roll
        // velocity is commanded in full; below it the commanded velocity ramps linearly to zero by
        // DeadbandLowFraction of it, so the craft coasts to a stop and the actuator stops dithering
        // against measured-rate jitter once pointed. Formerly the logistic attenuation half-width; see the
        // deadband note by DeadbandLowFraction.
        public double RollAttenuationAngle { get; set; }

        // Pointing deadband high angle (degrees) for the coupled pitch/yaw group. As RollAttenuationAngle,
        // but keyed on the 2D pitch/yaw predicted stopping-point magnitude.
        public double PitchYawAttenuationAngle { get; set; }

        public double RollStartAngle { get; set; }

        public double RollEngageAngle { get; set; }

        public bool AutoTune { get; set; }

        public Services.OscillationControl PitchYawOscillationControl {
            get { return pitchYawOscillationControl; }
            set { pitchYawOscillationControl = value; }
        }

        public Services.OscillationControl RollOscillationControl {
            get { return rollOscillationControl; }
            set { rollOscillationControl = value; }
        }

        public double PitchYawOscillationFrequency {
            get { return pitchYawOscillationFrequency; }
            set { pitchYawOscillationFrequency = value; }
        }

        public double RollOscillationFrequency {
            get { return rollOscillationFrequency; }
            set { rollOscillationFrequency = value; }
        }

        public double OscillationNotchQ {
            get { return oscillationNotchQ; }
            set { oscillationNotchQ = value; }
        }

        public double OscillationBandwidthFloor {
            get { return oscillationBandwidthFloor; }
            set { oscillationBandwidthFloor = value; }
        }

        public double OscillationDetectionThreshold {
            get { return oscillationDetectionThreshold; }
            set { oscillationDetectionThreshold = value; }
        }

        // Manual per-axis (pitch, roll, yaw) override in [0,1] forcing the latched flexible-craft hold
        // mitigation on regardless of pointing error (OR'd into the hold gate). 0 = no override.
        public Vector3d OscillationControlLevel {
            get { return oscillationControlLevel; }
            set { oscillationControlLevel = value; }
        }

        // About-mean envelope of the delivered command above which a latched axis is treated as still
        // limit-cycling and the hold mitigation is engaged regardless of pointing error.
        public double OscillationControlThreshold {
            get { return oscControlThreshold; }
            set { oscControlThreshold = value; }
        }

        // Read-only per-group about-mean control-output oscillation envelope (pitch/yaw coupled, roll).
        public double PitchYawControlOscillation {
            get { return Math.Max (controlOscEnvelope.x, controlOscEnvelope.z); }
        }

        public double RollControlOscillation {
            get { return controlOscEnvelope.y; }
        }

        // Read-only observability into the (otherwise hidden) flexible-craft detector state, so a
        // user can see whether a craft has been deemed wobbly.
        public Vector3d OscillationLevel {
            get { return chatterLevel; }
        }

        // Pitch (0) and yaw (2) latch together (see UpdateChatterDetector), so either reports the
        // pitch/yaw group.
        public bool PitchYawOscillationLatched {
            get { return chatterLatched [0] || chatterLatched [2]; }
        }

        public bool RollOscillationLatched {
            get { return chatterLatched [1]; }
        }

        // Estimated mode frequency (Hz) per axis group; the estimator runs in all modes, so this is
        // observable even under Off/Notch/LowPass, and is NaN only until the estimator first
        // acquires (the held value persists thereafter, see pitchYawHeldHz).
        public double PitchYawOscillationDetectedFrequency {
            get { return pitchYawHeldHz; }
        }

        public double RollOscillationDetectedFrequency {
            get { return rollHeldHz; }
        }

        // Measured angular velocity (raw, roll-invariant frame, rad/s) in the controller's internal
        // sign convention. Recorded each tick for the in-game info window.
        public Vector3d MeasuredAngularVelocity {
            get { return logRawOmegaRi; }
        }

        // Target angular velocity (roll-invariant frame, rad/s) the inner loop is tracking this tick.
        public Vector3d TargetAngularVelocity {
            get { return logTargetRi; }
        }

        // Per-axis hold-gated mitigation weight in [0,1] (suppressionRamp · holdFactor): how fully
        // the latched flexible-craft hold mitigation (bandwidth floor + feedforward cut) is engaged.
        public Vector3d MitigationLevel {
            get { return new Vector3d (mitigationLevel [0], mitigationLevel [1], mitigationLevel [2]); }
        }

        // Active suppression tool on an axis: 0 none, 1 notch, 2 low-pass (mirrors the diagnostic
        // tool glyph). Pitch (0) and yaw (2) share the pitch/yaw group's tool.
        public int ActiveSuppressionTool (int axis)
        {
            if (notchActiveAxis [axis])
                return 1;
            if (rateFilterValid [axis])
                return 2;
            return 0;
        }

        // The chatterLevel at which an axis latches as flexible (see UpdateChatterDetector). Exposed
        // so the info UI can colour the OscillationLevel readout against the same threshold.
        public double OscillationLatchThreshold {
            get { return ChatterLatchThreshold; }
        }

        public bool DiagnosticLogging {
            get { return diagnosticLogging; }
            set {
                if (value) {
                    lock (diagnosticLogLock) {
                        diagnosticLog.Clear ();
                    }
                }
                diagnosticLogging = value;
            }
        }

        public string GetDiagnosticLog ()
        {
            lock (diagnosticLogLock) {
                return diagnosticLog.ToString ();
            }
        }

        public Vector3d Overshoot {
            get { return overshoot; }
            set {
                overshoot = value;
                UpdatePIDParameters ();
            }
        }

        public Vector3d TimeToPeak {
            get { return timeToPeak; }
            set {
                timeToPeak = value;
                UpdatePIDParameters ();
            }
        }

        public double SoftStartTime {
            get { return softStartTime; }
            set { softStartTime = value; }
        }

        void UpdatePIDParameters ()
        {
            for (int i = 0; i < 3; i++) {
                var logOvershoot = Math.Log (overshoot [i]);
                var sqLogOvershoot = logOvershoot * logOvershoot;
                var zeta = Math.Sqrt (sqLogOvershoot / (Math.PI * Math.PI + sqLogOvershoot));
                var omega = Math.PI / (timeToPeak [i] * Math.Sqrt (1.0 - zeta * zeta));
                twiceZetaOmega [i] = 2 * zeta * omega;
                omegaSquared [i] = omega * omega;
            }
        }

        public void Reset ()
        {
            ReferenceFrame = vessel.SurfaceReferenceFrame;
            MaxAngularVelocity = new Vector3d (1, 1, 1);
            RollAttenuationAngle = 1.0;
            PitchYawAttenuationAngle = 1.0;
            RollStartAngle = 20.0;
            RollEngageAngle = 15.0;
            AutoTune = true;
            pitchYawOscillationControl = Services.OscillationControl.Automatic;
            rollOscillationControl = Services.OscillationControl.Automatic;
            pitchYawOscillationFrequency = DefaultOscillationFrequency;
            rollOscillationFrequency = DefaultOscillationFrequency;
            oscillationNotchQ = DefaultNotchQ;
            oscillationBandwidthFloor = DefaultBandwidthFloor;
            oscillationDetectionThreshold = DefaultChatterDetectThreshold;
            oscillationControlLevel = Vector3d.zero;
            oscControlThreshold = DefaultOscControlThreshold;
            Overshoot = new Vector3d (0.01, 0.01, 0.01);
            // TimeToPeak sets the inner-loop bandwidth via omega0 = pi / (TimeToPeak * sqrt(1 - zeta^2)).
            // Increasing it lowers the bandwidth, which is the lever for large, structurally flexible
            // vehicles: when the bandwidth approaches the structural resonance frequency (e.g. the
            // Ariane rocket, ~10 rad/s) the PID saturates in response to structural angular velocity
            // oscillations and drives the bending mode. Such craft need a larger TimeToPeak. (The
            // adaptive flexible-craft handling normally suppresses this automatically; see
            // UpdateChatterDetector.)
            TimeToPeak = new Vector3d (1, 1, 1);
            SoftStartTime = DefaultSoftStartTime;
            targetSmoothingTime = 0;
            DiagnosticLogging = false;
            SetTarget (0, 0, double.NaN);
            Start ();
        }

        public void Start ()
        {
            engageFixedTime = Time.fixedTime;
            // Hold the current commanded target on engage — no phantom slew (engagement transients
            // are handled separately by the soft-start). Smoothing only applies to later changes.
            effectiveRotation = targetRotation;
            slewSpeed = 0;
            slewPending = false;
            PitchPID.ResetState ();
            RollPID.ResetState ();
            YawPID.ResetState ();
            pointRotationValid = false;
            antipodeLatched = false;
            prevTargetRiValid = false;
            smoothedFfRi = Vector3d.zero;
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
            if (AutoTune)
                DoAutoTune (vessel.AvailableTorqueVectors.Item1, vessel.MomentOfInertiaVector);
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            var internalVessel = vessel.InternalVessel;
            var torque = vessel.AvailableTorqueVectors.Item1;
            var moi = vessel.MomentOfInertiaVector;
            var dt = Time.fixedDeltaTime;

            // While the vessel sits on the launch clamps (PRELAUNCH) the autopilot is engaged but
            // must not act. The craft is physically held and the engines are unlit, so available
            // torque is near-zero; running the loop would autotune enormous gains against that tiny
            // torque (Kp ∝ moi/torque) and saturate the actuators against sensor jitter or a sub-
            // degree pointing error — a full-deflection command that is then delivered as a kick the
            // instant the clamps release and the gains collapse onto the now-large engine torque.
            // Instead hold the controls at zero and keep the whole control loop in its freshly-
            // engaged default state (Start re-pins the soft-start clock and clears all loop state),
            // re-running it each tick so the engagement soft-start fade-in begins at clamp release
            // rather than at engagement. Holding on the pad is thus equivalent to engaging the moment
            // the clamps drop.
            if (internalVessel.situation == Vessel.Situations.PRELAUNCH) {
                Start ();
                state.Pitch = 0;
                state.Roll = 0;
                state.Yaw = 0;
                logRawOmegaRi = Vector3d.zero;
                logTargetRi = Vector3d.zero;
                return;
            }

            // Target smoothing: advance the effective target (what the control loop tracks) toward
            // the commanded target. With smoothing enabled, a change to the target ramps the
            // effective target linearly (constant-rate slerp) over TargetSmoothingTime seconds,
            // rather than stepping instantly — letting a slow control loop drive a smooth maneuver
            // (e.g. a gravity turn) without exciting oscillation from stepwise target changes.
            if (slewPending) {
                slewSpeed = targetSmoothingTime > 0
                    ? GeometryExtensions.Angle (effectiveRotation, targetRotation) / targetSmoothingTime
                    : 0;
                slewPending = false;
            }
            if (targetSmoothingTime > 0) {
                effectiveRotation = GeometryExtensions.RotateTowards (
                    effectiveRotation, targetRotation, slewSpeed * dt);
            } else {
                effectiveRotation = targetRotation;
            }

            // Engagement soft-start: a smoothstep 0→1 over SoftStartTime seconds from the last
            // Start(). The final actuator command is scaled by this so engagement fades the control
            // in rather than stepping to a near-max kick. Smoothstep (not a one-pole) has zero slope
            // at t=0, so the onset is gentlest exactly when the loop is furthest from steady state.
            var softStartLinear = softStartTime > 0
                ? Math.Min (1.0, Math.Max (0.0, (Time.fixedTime - engageFixedTime) / softStartTime))
                : 1.0;
            var softStart = softStartLinear * softStartLinear * (3.0 - 2.0 * softStartLinear);

            // Compute the roll-invariant frame: a frame sharing the vessel's nose direction but
            // with zero roll relative to the AP reference frame. Expressing both the target and
            // current angular velocities in this frame means roll corrections do not disturb the
            // path taken to point the vessel.
            Vector3d currentDirection;
            double phi, cosPhi, sinPhi;
            ComputeRollInvariantFrame (internalVessel, out currentDirection, out phi, out cosPhi, out sinPhi);

            // Measure the raw angular velocity (root-part rigidbody). On a structurally flexible
            // craft it carries the bending mode on top of the rigid-body rate. The chatter detector
            // and the frequency trackers always see this raw rate; only the control loops see the
            // suppressed rate computed below (see the signal-routing note in the design doc).
            var currentRaw = (Vector3d)ComputeCurrentAngularVelocity ();

            // Update the structural-chatter detector from the raw rate. In Automatic mode the latch
            // it produces decides *whether* suppression is applied; the frequency estimator below
            // decides *which* tool (notch vs low-pass). A rigid craft never latches.
            UpdateChatterDetector (currentRaw, torque, moi, dt);

            // Apply the per-group selector's detector side: Off forces the group rigid for control
            // (latch/level cleared, no suppression) while leaving the estimator running; Automatic
            // leaves the detector's verdict; Notch/LowPass bypass the detector (the tool is forced in
            // the suppression selection below). Pitch (0) and yaw (2) share pitchYawOscillationControl.
            ApplyOscillationOverride (0, pitchYawOscillationControl);
            ApplyOscillationOverride (2, pitchYawOscillationControl);
            ApplyOscillationOverride (1, rollOscillationControl);

            // Feed the frequency trackers the oscillation in the raw rate every tick, unconditionally
            // — they cost only a handful of adds, so an estimate is always warm the moment an axis
            // latches. High-pass the rate (subtract a slow mean) so the trackers see the structural
            // oscillation without DC or slew trends, and without the high-frequency bias a raw Δω
            // (derivative) would impose. The pitch/yaw tracker is fed whichever transverse axis
            // currently carries more oscillation (larger envelope, the lateral mode being
            // ~axisymmetric); roll is fed directly.
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
            // Feed the pitch/yaw tracker whichever transverse axis currently carries more oscillation
            // (the lateral mode is ~axisymmetric, so either reads it); roll is fed directly.
            var pyHp = emaAbsHp.x >= emaAbsHp.z ? hp.x : hp.z;
            pitchYawFreqTracker.Update (pyHp, dt);
            rollFreqTracker.Update (hp.y, dt);
            // Latch the last acquired estimate so suppression quieting the mode does not lose it.
            if (!double.IsNaN (pitchYawFreqTracker.EstimatedHz))
                pitchYawHeldHz = pitchYawFreqTracker.EstimatedHz;
            if (!double.IsNaN (rollFreqTracker.EstimatedHz))
                rollHeldHz = rollFreqTracker.EstimatedHz;

            // Select and apply the suppression tool per axis group, producing the rate the control
            // loops consume. Each axis is notched, low-passed, or passed through — never both — with
            // the inactive filter's state held reset so a regime change injects no transient. The
            // per-axis suppressionActiveAxis record written here is read by DoAutoTuneAxis.
            int pitchYawTool;
            double pitchYawFreq;
            SelectSuppressionTool (pitchYawOscillationControl, PitchYawOscillationLatched,
                pitchYawHeldHz, pitchYawOscillationFrequency,
                out pitchYawTool, out pitchYawFreq);
            int rollTool;
            double rollFreq;
            SelectSuppressionTool (rollOscillationControl, RollOscillationLatched,
                rollHeldHz, rollOscillationFrequency, out rollTool, out rollFreq);
            var current = new Vector3d (
                ApplySuppression (0, currentRaw.x, pitchYawTool, pitchYawFreq, dt, pitchNotch),
                ApplySuppression (1, currentRaw.y, rollTool, rollFreq, dt, rollNotch),
                ApplySuppression (2, currentRaw.z, pitchYawTool, pitchYawFreq, dt, yawNotch));

            // Light measured-rate low-pass on a detector-firing but *unlatched* axis (see
            // DetectorRateFilterTimeConstant). The suppression above only runs on a latched axis; here the
            // axis passed through, but its rate still carries the root-part jitter that the full-bandwidth
            // inner loop turns into actuator chatter and that the stopping-distance term re-injects into
            // the feedforward. Blend a lightly low-passed copy in by chatterLevel — rigid axes (level ~0)
            // untouched, latched axes skipped (the heavier suppression handles them) — so both the inner
            // loop and the outer stopping term below act on a quieter rate.
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

            // Ramp the latched bandwidth reduction and output smoothing in/out per axis (gated on
            // suppression being active — the persistent latch, not the decaying chatterLevel — so
            // they hold for as long as the craft is treated as flexible and do not step at engage).
            var rampBeta = 1.0 - Math.Exp (-dt / BandwidthRampTimeConstant);
            for (int i = 0; i < 3; i++)
                suppressionRamp [i] +=
                    rampBeta * ((suppressionActiveAxis [i] ? 1.0 : 0.0) - suppressionRamp [i]);

            // Current and target angular velocities, both expressed in the roll-invariant frame.
            var currentRi = ToRollInvariant (current, cosPhi, sinPhi);
            logRawOmegaRi = ToRollInvariant (currentRaw, cosPhi, sinPhi);
            var target = ComputeTargetAngularVelocity (torque, moi, current, currentDirection, cosPhi, sinPhi);

            // Nominal target: the velocity profile with the measured rate set to zero, so it depends
            // only on the attitude error. A latched, holding axis tracks this instead of `target`
            // (blended by the hold gate below) — at the floored bandwidth the stopping-distance term
            // ω/bandwidth would re-inject the residual rate into the setpoint and the large floored
            // gain would amplify it into a limit cycle; the rate-independent nominal target removes
            // that. While slewing the axis tracks the full `target` (with its stopping term) so it
            // brakes cleanly.
            var targetNominal = ComputeTargetAngularVelocity (torque, moi, Vector3d.zero, currentDirection, cosPhi, sinPhi);

            // Roll setpoint is already weighted to zero inside ComputeTargetAngularVelocity when the
            // vessel is far from the direction target; clear the integral there to prevent windup.
            if (!rollControlled) {
                target.y = 0;
                targetNominal.y = 0;
            } else {
                ClearRollWindupIfDisengaged (currentDirection);
            }

            // Continuous hold gate: 1 while holding (pointing error ≤ HoldErrorFull), 0 while slewing
            // (≥ HoldErrorNone), linear between. Combined with suppressionRamp it gives the per-axis
            // mitigation weight — only a latched axis that is also holding fully tracks the nominal
            // target and cuts the feedforward.
            var pointingError = Vector3.Angle (currentDirection, EffectiveTargetDirection);
            var holdFactor = Math.Min (1.0, Math.Max (0.0,
                (HoldErrorNone - pointingError) / (HoldErrorNone - HoldErrorFull)));
            // Oscillation-control back-off, OR'd into the hold gate via max() below: lets the latched
            // mitigation engage independent of pointing error — the hold gate's blind spot during a
            // maneuver, where a released gate restores the feedforward that re-drives the bending mode.
            // The trigger is the about-mean envelope of the delivered command (built from the previous
            // tick, since this tick's command does not exist yet): a sustained limit cycle has a large
            // envelope while a steady slew (one-sign ramp, tracked by the trim mean) does not. It rises
            // fast / decays slow so a one-shot transient does not pin it, only a latched axis can
            // trigger, and the manual OscillationControlLevel is a floor under the automatic level.
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
            // Pitch (x) and yaw (z) are coupled (mirror the chatter detector); roll (y) on its own.
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

            // Per-axis setpoint the loop tracks: full target eased toward the nominal target by the gate.
            var pidTarget = new Vector3d (
                target.x + gate.x * (targetNominal.x - target.x),
                target.y + gate.y * (targetNominal.y - target.y),
                target.z + gate.z * (targetNominal.z - target.z));
            logTargetRi = pidTarget;

            // Acceleration feedforward: differentiate the velocity setpoint numerically to get the
            // angular acceleration needed to stay on the bang-bang trajectory, then normalise by
            // α_max = torque/moi so the feedforward is a control fraction in [-1, 1]. Skipped on the
            // first tick (prevTargetRiValid == false) to avoid a spike.
            var rawFfRi = Vector3d.zero;
            if (prevTargetRiValid) {
                var alphaPitch = moi [0] > 0 ? torque [0] / moi [0] : 0.0;
                var alphaRoll  = moi [1] > 0 ? torque [1] / moi [1] : 0.0;
                var alphaYaw   = moi [2] > 0 ? torque [2] / moi [2] : 0.0;
                if (alphaPitch > 0) rawFfRi.x = (pidTarget.x - prevTargetRi.x) / (dt * alphaPitch);
                if (alphaRoll  > 0) rawFfRi.y = (pidTarget.y - prevTargetRi.y) / (dt * alphaRoll);
                if (alphaYaw   > 0) rawFfRi.z = (pidTarget.z - prevTargetRi.z) / (dt * alphaYaw);
            }
            prevTargetRi = pidTarget;
            prevTargetRiValid = true;

            // Low-pass filter the feedforward. The velocity setpoint has slope discontinuities — the
            // min()/max() switches in the bang-bang profile (the velocity cap and the quad/linear
            // stopping term) and the sign flip through the target — and differentiating those
            // produces single-tick steps in the raw feedforward that, once summed with the PID
            // output and clamped, briefly saturate the actuators. A short first-order filter removes
            // these transients; the resulting lag is a few physics ticks and the PI loop absorbs it.
            var ffBeta = 1.0 - Math.Exp (-dt / FeedforwardSmoothTimeConstant);
            smoothedFfRi = new Vector3d (
                smoothedFfRi.x + ffBeta * (rawFfRi.x - smoothedFfRi.x),
                smoothedFfRi.y + ffBeta * (rawFfRi.y - smoothedFfRi.y),
                smoothedFfRi.z + ffBeta * (rawFfRi.z - smoothedFfRi.z));
            // Cut the feedforward by the hold gate: it is an open-loop plant inversion (gain ∝
            // frequency) and is the loop path most able to drive a flexible mode once the bandwidth is
            // floored, but only a holding flexible axis needs it gone — while slewing the axis keeps
            // its feedforward to track the manoeuvre, and a rigid axis keeps it throughout.
            var ffRi = new Vector3d (
                smoothedFfRi.x * (1.0 - gate.x),
                smoothedFfRi.y * (1.0 - gate.y),
                smoothedFfRi.z * (1.0 - gate.z));

            UpdateSmoothedTorque (torque);

            // Autotune the controllers if enabled (uses smoothed torque to avoid gain spikes)
            if (AutoTune)
                DoAutoTune (smoothedTorque, moi);

            // Zero the integral terms throughout the engagement soft-start (the on-pad PRELAUNCH
            // case is handled by the early return above). During the fade the scaled-down command
            // under-drives the plant, so the error persists and the integrator would wind up — then
            // deliver the very kick the soft-start removes the moment the ramp completes. Holding it
            // cleared means integration starts from zero exactly at softStart == 1, with no step (the
            // proportional/feedforward output is faded too).
            if (softStart < 1.0) {
                PitchPID.ClearIntegralTerm ();
                RollPID.ClearIntegralTerm ();
                YawPID.ClearIntegralTerm ();
            }

            // Inner rate loop (the inner stage of the cascade). The outer loop above turned the
            // attitude error into a target angular velocity; this stage drives the *suppressed*
            // measured rate (currentRi) onto that target. Each PI is physics-normalised — its
            // autotuned gains carry the moi/torque factor, so the closed rate-loop bandwidth is set
            // by TimeToPeak/Overshoot and is independent of the craft's authority. Because it tracks
            // the suppressed rate, the loop has no gain at the bending frequency and cannot excite a
            // flexible structure. Pitch/yaw run in the roll-invariant frame; roll on the y-axis
            // (unchanged by the frame rotation). Add the acceleration feedforward to each output,
            // then clamp before converting pitch/yaw back to the body frame.
            var virtualPitch = (RunAxis (PitchPID, pidTarget.x, currentRi.x, torque [0], dt) + ffRi.x).Clamp (-1, 1);
            var virtualRoll = (RunAxis (RollPID, pidTarget.y, currentRi.y, torque [1], dt) + ffRi.y).Clamp (-1, 1);
            var virtualYaw = (RunAxis (YawPID, pidTarget.z, currentRi.z, torque [2], dt) + ffRi.z).Clamp (-1, 1);
            var bodyControl = FromRollInvariant (new Vector3d (virtualPitch, 0, virtualYaw), cosPhi, sinPhi);

            // Gyroscopic feedforward: the per-axis plant model (τ = I·ω̇) ignores the ω×(Iω) term in
            // Euler's rigid-body equation. Add a control fraction that cancels it, in the body frame
            // where the inertia and available torque are per-axis, then sum with the control and
            // clamp to [-1, 1].
            var gyro = GyroscopicFeedforward (current, moi, torque);
            // Output smoothing: on a latched axis blend in a low-passed copy of the final command
            // (ramped by suppressionRamp) to cap residual control chatter; rigid axes pass through.
            state.Pitch = (float)(softStart * SmoothOutput (0, (bodyControl.x + gyro.x).Clamp (-1, 1), dt));
            state.Roll = (float)(softStart * SmoothOutput (1, (virtualRoll + gyro.y).Clamp (-1, 1), dt));
            state.Yaw = (float)(softStart * SmoothOutput (2, (bodyControl.z + gyro.z).Clamp (-1, 1), dt));

            // Store the delivered command for next tick's control-output oscillation envelope.
            prevControl = new Vector3d (state.Pitch, state.Roll, state.Yaw);
            prevControlValid = true;

            if (diagnosticLogging)
                LogDiagnostics (torque, moi, phi, currentRi, target, ffRi, gyro, currentDirection, state);
        }

        /// <summary>
        /// Compute the roll-invariant frame for the current tick. phi is the roll angle of the
        /// vessel body frame relative to the roll-invariant frame.
        /// </summary>
        /// <remarks>
        /// The body x-axis expressed in the roll-invariant frame is R_y(phi)*(1,0,0) =
        /// (cos(phi), 0, -sin(phi)), so phi = atan2(-bodyX_ri.z, bodyX_ri.x).
        /// </remarks>
        void ComputeRollInvariantFrame (Vessel internalVessel, out Vector3d currentDirection,
            out double phi, out double cosPhi, out double sinPhi)
        {
            currentDirection = ReferenceFrame.DirectionFromWorldSpace (internalVessel.ReferenceTransform.up);
            var Q_vessel_ap = ReferenceFrame.RotationFromWorldSpace (internalVessel.ReferenceTransform.rotation);
            // Pointing-only rotation (up -> nose), carried forward continuously through the antipode
            // singularity. FromToRotation(up, nose) is hypersensitive to transverse motion when nose
            // is near -up and would whip the frame ~180 deg in a tick; instead propagate by the
            // minimal rotation between consecutive nose directions, which is always a small,
            // well-conditioned angle. Seeded from the fixed reference on the first tick after Start.
            QuaternionD Q_point_ap;
            if (pointRotationValid) {
                var delta = GeometryExtensions.FromToRotation (prevPointDirection, currentDirection);
                Q_point_ap = (delta * pointRotation).Normalize ();
            } else {
                Q_point_ap = GeometryExtensions.FromToRotation (Vector3d.up, currentDirection);
            }
            pointRotation = Q_point_ap;
            prevPointDirection = currentDirection;
            pointRotationValid = true;
            var bodyXInRI = (Q_point_ap.Inverse () * Q_vessel_ap) * new Vector3d (1, 0, 0);
            phi = Math.Atan2 (-bodyXInRI.z, bodyXInRI.x);
            cosPhi = Math.Cos (phi);
            sinPhi = Math.Sin (phi);
        }

        /// <summary>
        /// Rotate a vector from the vessel body frame into the roll-invariant frame, i.e. strip
        /// vessel roll phi (R_y(phi)). The y-axis (nose/roll axis) is unchanged by the rotation.
        /// </summary>
        static Vector3d ToRollInvariant (Vector3d v, double cosPhi, double sinPhi)
        {
            return new Vector3d (
                v.x * cosPhi + v.z * sinPhi,
                v.y,
                -v.x * sinPhi + v.z * cosPhi);
        }

        /// <summary>
        /// Rotate a vector from the roll-invariant frame back into the vessel body frame (R_y(-phi)).
        /// </summary>
        static Vector3d FromRollInvariant (Vector3d v, double cosPhi, double sinPhi)
        {
            return new Vector3d (
                v.x * cosPhi - v.z * sinPhi,
                v.y,
                v.x * sinPhi + v.z * cosPhi);
        }

        /// <summary>
        /// Convert a direction vector from the autopilot reference frame to the vessel body frame.
        /// </summary>
        Vector3d ApToBody (Vector3d v)
        {
            return vessel.ReferenceFrame.DirectionFromWorldSpace (ReferenceFrame.DirectionToWorldSpace (v));
        }

        /// <summary>
        /// Weight in [0,1] that blends roll control in as the direction error decreases from
        /// RollStartAngle to RollEngageAngle.
        /// </summary>
        double RollWeight (double dirError)
        {
            return Math.Min (1.0, Math.Max (0.0, (RollStartAngle - dirError) / (RollStartAngle - RollEngageAngle)));
        }

        /// <summary>
        /// Clear the roll PID integral while roll control is suppressed (vessel far from the
        /// direction target) to prevent windup that would kick when roll control re-engages.
        /// </summary>
        void ClearRollWindupIfDisengaged (Vector3d currentDirection)
        {
            var dirError = Vector3.Angle (currentDirection, EffectiveTargetDirection);
            if (RollWeight (dirError) == 0.0)
                RollPID.ClearIntegralTerm ();
        }

        /// <summary>
        /// Update the one-sided torque smoothing: track increases immediately (gains going down is
        /// safe) but decay decreases at τ≈0.5s so a sudden torque drop (e.g. engine shutdown while a
        /// small reaction wheel keeps torque > 0) does not cause a single-tick gain spike. The
        /// velocity profile still uses the actual torque so the setpoint immediately reflects the
        /// reduced authority.
        /// </summary>
        void UpdateSmoothedTorque (Vector3d torque)
        {
            var decay = Math.Exp (-Time.fixedDeltaTime / TorqueSmoothTimeConstant);
            smoothedTorque = new Vector3d (
                Math.Max (torque.x, smoothedTorque.x * decay),
                Math.Max (torque.y, smoothedTorque.y * decay),
                Math.Max (torque.z, smoothedTorque.z * decay));
        }

        /// <summary>
        /// Run a single PID axis. When the axis has no available torque, zero its output and clear
        /// the integral so accumulated history does not cause a transient when authority returns
        /// (e.g. engine restart after shutdown).
        /// </summary>
        static double RunAxis (PIDController pid, double target, double current, double torque, double dt)
        {
            if (torque > 0)
                return pid.Update (target, current, dt);
            pid.ClearIntegralTerm ();
            return 0;
        }

        void LogDiagnostics (Vector3d torque, Vector3d moi, double phi, Vector3d currentRi,
            Vector3d target, Vector3d ffRi, Vector3d gyro, Vector3d currentDirection, PilotAddon.ControlInputs state)
        {
            // Tracking error to the target the loop is actually following (the slewed/effective
            // target, not the commanded one), so it stays small while a smoothed change is fed in.
            var dirErr = Vector3.Angle (currentDirection, EffectiveTargetDirection);
            var effectivePhr = effectiveRotation.PitchHeadingRoll ();
            var line = string.Format (
                "[KRPC.AP] t={0:F3} err={1:F2}deg" +
                " torque=({2:F4},{3:F4},{4:F4})" +
                " moi=({5:F6},{6:F6},{7:F6})" +
                " alpha=({8:F3},{9:F3},{10:F3})" +
                " ang_err=({11:F2},{12:F2},{13:F2})deg" +
                " phi={14:F2}deg" +
                " omega_ri=({15:F4},{16:F4},{17:F4})" +
                " tgt_omega_ri=({18:F4},{19:F4},{20:F4})" +
                " ff_ri=({21:F3},{22:F3},{23:F3})" +
                " gyro=({24:F3},{25:F3},{26:F3})" +
                " Kp=({27:F4},{28:F4},{29:F4}) Ki=({30:F4},{31:F4},{32:F4})" +
                " ctrl=(p={33:F3},r={34:F3},y={35:F3})" +
                " omega_raw=({36:F4},{37:F4},{38:F4})" +
                " chatter=({39:F3},{40:F3},{41:F3})" +
                " tool=({42},{43},{44})" +
                " fdet=({45:F3},{46:F3})" +
                " bwtgt=({47:F3},{48:F3},{49:F3})" +
                " oscctl=({50:F3},{51:F3}) bko=({52:F2},{53:F2},{54:F2})" +
                " tgt_smooth=({55:F2},{56:F2})deg",
                Time.fixedTime, dirErr,
                torque.x, torque.y, torque.z,
                moi.x, moi.y, moi.z,
                moi.x > 0 ? torque.x / moi.x : 0, moi.y > 0 ? torque.y / moi.y : 0, moi.z > 0 ? torque.z / moi.z : 0,
                logAngles.x, logAngles.y, logAngles.z,
                phi * 180.0 / Math.PI,
                currentRi.x, currentRi.y, currentRi.z,
                target.x, target.y, target.z,
                ffRi.x, ffRi.y, ffRi.z,
                gyro.x, gyro.y, gyro.z,
                PitchPID.Kp, RollPID.Kp, YawPID.Kp,
                PitchPID.Ki, RollPID.Ki, YawPID.Ki,
                state.Pitch, state.Roll, state.Yaw,
                logRawOmegaRi.x, logRawOmegaRi.y, logRawOmegaRi.z,
                chatterLevel.x, chatterLevel.y, chatterLevel.z,
                notchActiveAxis [0] ? "n" : rateFilterValid [0] ? "l" : "-",
                notchActiveAxis [1] ? "n" : rateFilterValid [1] ? "l" : "-",
                notchActiveAxis [2] ? "n" : rateFilterValid [2] ? "l" : "-",
                pitchYawHeldHz, rollHeldHz,
                suppressionRamp [0] * oscillationBandwidthFloor,
                suppressionRamp [1] * oscillationBandwidthFloor,
                suppressionRamp [2] * oscillationBandwidthFloor,
                Math.Max (controlOscEnvelope.x, controlOscEnvelope.z), controlOscEnvelope.y,
                oscControlBackoff [0], oscControlBackoff [1], oscControlBackoff [2],
                effectivePhr.x, effectivePhr.y);
            UnityEngine.Debug.Log (line);
            lock (diagnosticLogLock) {
                diagnosticLog.AppendLine (line);
            }
        }

        /// <summary>
        /// Compute current angular velocity in pitch,roll,yaw axes
        /// </summary>
        Vector3 ComputeCurrentAngularVelocity ()
        {
            var worldAngularVelocity = vessel.InternalVessel.GetComponent<Rigidbody> ().angularVelocity;
            var localAngularVelocity = ReferenceFrame.AngularVelocityFromWorldSpace (worldAngularVelocity);
            // Express the measured angular velocity in the controller's sign convention.
            //
            // The direction error is built by FromToRotation(current, target), whose axis is the
            // right-handed cross product current x target (see GeometryExtensions.FromToRotation).
            // A positive error component therefore corresponds to a right-handed rotation that
            // carries the nose toward the target. The velocity profile then commands
            // omega_target = -sign(theta) * speed (the -Math.Sign(...) terms in ComputeAxisVelocity
            // and ComputePitchYawVelocity), i.e. it defines the controller's positive angular-
            // velocity direction as the *left-handed* sense about each axis.
            //
            // Rigidbody.angularVelocity is in the geometric (Unity) sense, so it is negated here to
            // put the measurement into that same left-handed controller convention. The negation and
            // the -sign(theta) command are a matched pair: the PI loop compares target against
            // measurement, so both must use the same sign. Removing this negation alone makes them
            // disagree and the loop diverges; removing both negations together would be equivalent.
            return -ApToBody (localAngularVelocity);
        }

        /// <summary>
        /// Second-order low-pass on one axis of the measured angular velocity, two cascaded
        /// first-order sections (each with time constant <paramref name="tau"/>) for a
        /// critically-damped ~12 dB/octave roll-off. This is the high-frequency suppression tool: a
        /// notch cannot reject a near-Nyquist mode, but such a mode is far above the sub-Hz control
        /// band, so a corner well below it (derived from the estimated frequency) kills it while
        /// adding negligible in-band lag. The state is seeded with the first sample so engaging on an
        /// already-rotating vessel produces no startup transient.
        /// </summary>
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

        /// <summary>
        /// Select the suppression tool and its frequency for an axis group. <c>tool</c> is 0 (none),
        /// 1 (notch) or 2 (low-pass). In <c>Automatic</c> mode suppression is active only while the
        /// group is latched, and is routed by the estimated frequency (notch below
        /// <see cref="DefaultSplitFrequency"/>, low-pass above), seeded at the group's manual
        /// frequency until the estimator acquires. <c>Notch</c>/<c>LowPass</c> force that tool at the
        /// manual frequency; <c>Off</c> applies nothing.
        /// </summary>
        void SelectSuppressionTool (Services.OscillationControl mode, bool latched, double detectedHz,
            double manualHz, out int tool, out double freq)
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
                    // Latched but the estimator has not locked: clean the rate with a frequency-
                    // independent broadband low-pass (~LowPassCornerMin) rather than a notch at the
                    // seed, which would be placed wrong if the true mode is elsewhere. The estimator
                    // is unreliable on some craft, so this fallback (plus the gain-stabilising
                    // bandwidth floor) is what makes suppression robust without a confident estimate.
                    tool = 2;
                    freq = LowPassCornerMin * DefaultLowPassSeparation;
                } else {
                    freq = detectedHz;
                    tool = freq < DefaultSplitFrequency ? 1 : 2;
                }
                break;
            }
        }

        /// <summary>
        /// Apply the selected suppression tool to one axis, holding the inactive filter's state reset
        /// so a regime change injects no transient, and recording whether the notch is active (and at
        /// what frequency) for the bandwidth reduction in <see cref="DoAutoTuneAxis"/>.
        /// </summary>
        double ApplySuppression (int index, double x, int tool, double freq, double dt, BiquadNotchFilter notch)
        {
            if (tool == 1 && freq > 0) {
                rateFilterValid [index] = false;
                notchActiveAxis [index] = true;
                suppressionActiveAxis [index] = true;
                // Reconfigure only when the frequency has moved materially (>2%), Q changed, or the
                // sampling period changed, so a slowly drifting mode is tracked without thrashing.
                if (Math.Abs (freq - notch.ConfiguredFrequency) > 0.02 * notch.ConfiguredFrequency ||
                    notch.ConfiguredQ != oscillationNotchQ || notch.ConfiguredDt != dt)
                    notch.SetFrequency (freq, oscillationNotchQ, dt);
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
            // Pass-through (no suppression): hold both filters reset.
            notch.Reset ();
            rateFilterValid [index] = false;
            notchActiveAxis [index] = false;
            suppressionActiveAxis [index] = false;
            return x;
        }

        /// <summary>
        /// Blend a low-passed copy of the final actuator command in by <c>suppressionRamp</c> on a
        /// latched axis, to cap residual control chatter. A rigid axis (ramp 0) passes through.
        /// </summary>
        double SmoothOutput (int index, double u, double dt)
        {
            // Blend weight and filter corner depend on the regime. A latched axis uses the ramped
            // suppression weight and the standard (2 Hz) corner — the flexible-craft path, unchanged. An
            // unlatched axis whose detector is firing (the noisy-but-controllable craft) blends by
            // chatterLevel at the lighter DetectorOutputFilterTimeConstant corner, smoothing the delivered
            // command without the phase cost of the 2 Hz filter destabilising the full-bandwidth loop. A
            // rigid axis (chatterLevel ~0, never latched) gets weight 0 and passes through.
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

        /// <summary>
        /// Detect a per-axis structural limit cycle and drive <c>chatterLevel</c> in [0,1].
        /// </summary>
        /// <remarks>
        /// The signature of a bending-mode limit cycle is the measured rate (sampled at the root
        /// part) changing tick-to-tick by more than the available torque could physically produce:
        /// rigid-body motion is bounded by <c>α·dt = (τ/I)·dt</c> at full authority, so a jump several
        /// times larger is structural oscillation, not a response to the controller. This is
        /// gain-independent and maneuver-independent — a legitimate full-authority slew (or a
        /// bang-bang sign reversal, where the rate is continuous) stays within <c>α·dt</c>, so it does
        /// not trip. The level rises quickly (<c>ChatterRiseTimeConstant</c>) when excitation appears
        /// and decays slowly (<c>ChatterDecayTimeConstant</c>); the slow release is essential because
        /// the reduced-bandwidth state is quiet and would otherwise immediately clear the detector and
        /// let the bandwidth climb back into the limit cycle.
        /// </remarks>
        void UpdateChatterDetector (Vector3d rawOmega, Vector3d torque, Vector3d moi, double dt)
        {
            if (!prevDetectorOmegaValid) {
                prevDetectorOmega = rawOmega;
                prevDetectorOmegaValid = true;
                return;
            }
            for (int i = 0; i < 3; i++) {
                var alpha = moi [i] > 0 ? torque [i] / moi [i] : 0.0;
                var deltaOmega = Math.Abs (rawOmega [i] - prevDetectorOmega [i]);
                var excited = alpha > 0 && deltaOmega > oscillationDetectionThreshold * alpha * dt ? 1.0 : 0.0;
                var timeConstant = excited > chatterLevel [i] ? ChatterRiseTimeConstant : ChatterDecayTimeConstant;
                var beta = 1.0 - Math.Exp (-dt / timeConstant);
                chatterLevel [i] += beta * (excited - chatterLevel [i]);
                // Latch the axis as flexible once the level confirms a limit cycle. The latch is the
                // controller's persistent memory that the craft is flexible; in Automatic mode it
                // gates whether suppression is applied (the estimator, which selects the tool, runs
                // ungated). A rigid craft never reaches the threshold (its rate never jumps beyond
                // rigid-body dynamics) so is never latched.
                if (chatterLevel [i] >= ChatterLatchThreshold)
                    chatterLatched [i] = true;
            }
            // Couple the two transverse axes (pitch x, yaw z). A long vehicle's first lateral bending
            // mode is essentially axisymmetric, so it limit-cycles in both planes; whichever plane
            // currently carries less of it can sit just under the Δω threshold and never latch on its
            // own, leaving its feedforward live to drive the structure (and, when the vehicle is
            // rolled, bleed into the other body axis). Once either transverse axis confirms flexible
            // the craft is flexible, so latch both. Roll (y) is longitudinal and handled on its own.
            if (chatterLatched [0] || chatterLatched [2])
                chatterLatched [0] = chatterLatched [2] = true;
            prevDetectorOmega = rawOmega;
        }

        /// <summary>
        /// Apply the detector side of a manual <see cref="Services.OscillationControl"/> override to
        /// a single axis. <c>Off</c> forces the axis rigid for control purposes (clears the latch and
        /// level so no suppression is applied), while the frequency estimator keeps running so the
        /// detected-frequency observable stays live. <c>Automatic</c> leaves the detector's verdict
        /// untouched; <c>Notch</c>/<c>LowPass</c> bypass the detector entirely (the tool is forced in
        /// <see cref="SelectSuppressionTool"/>), so they leave the latch alone here.
        /// </summary>
        void ApplyOscillationOverride (int index, Services.OscillationControl mode)
        {
            if (mode == Services.OscillationControl.Off) {
                chatterLevel [index] = 0;
                chatterLatched [index] = false;
            }
        }

        /// <summary>
        /// Gyroscopic feedforward in the body frame, returned as a per-axis control fraction.
        /// </summary>
        /// <remarks>
        /// The PID controllers and the autotuner model the plant as τ = I·ω̇ independently per axis,
        /// but the rigid-body equation of motion is τ = I·ω̇ + ω×(Iω). The cross term ω×(Iω) is a
        /// torque the controller would otherwise have to reject as a disturbance. This returns the
        /// control fraction that cancels it: -(ω×(Iω))ᵢ / τ_max,ᵢ per axis (a diagonal inertia is
        /// assumed, matching the rest of the controller). The term is quadratic in ω, so it is
        /// negligible at the low rates of normal attitude holding — including structural bending
        /// oscillation — and only matters for fast slews or strongly asymmetric inertia.
        ///
        /// ω is passed in the controller's negated sign convention (see ComputeCurrentAngularVelocity),
        /// but ω×(Iω) is quadratic in ω and so is invariant under that negation — it gives the correct
        /// body-frame gyroscopic torque either way.
        /// </remarks>
        Vector3d GyroscopicFeedforward (Vector3d omega, Vector3d moi, Vector3d torque)
        {
            var angularMomentum = new Vector3d (moi.x * omega.x, moi.y * omega.y, moi.z * omega.z);
            var gyroTorque = Vector3d.Cross (omega, angularMomentum);
            return new Vector3d (
                torque.x > 0 ? -gyroTorque.x / torque.x : 0.0,
                torque.y > 0 ? -gyroTorque.y / torque.y : 0.0,
                torque.z > 0 ? -gyroTorque.z / torque.z : 0.0);
        }

        /// <summary>
        /// Resolve the direction-error rotation axis through the 180° (antipodal) singularity.
        /// </summary>
        /// <remarks>
        /// <c>FromToRotation(current, target)</c> returns an arbitrary axis when the target is
        /// directly behind the nose (the minimum-arc rotation's plane is undefined), so a flip there
        /// would tumble in a random plane. When the vessel enters the antipode band already rotating,
        /// <em>latch</em> the flip-plane normal once — from the angular velocity perpendicular to the
        /// nose at that moment — and hold it for the rest of the pass. The commanded axis is then
        /// rebuilt each tick from that fixed plane, not from the live angular velocity: tracking the
        /// live rate instead lets any out-of-plane drift the inner loop introduces feed back on
        /// itself (the axis follows the drifting rate, which drives more drift), and the flip tumbles.
        /// With the plane held fixed the direction-error setpoint stays in-plane, so the rate loop
        /// actively damps out-of-plane motion instead of chasing it.
        ///
        /// Active only within <see cref="AntipodeBlendAngle"/> of antipodal. The plane is latched from
        /// the perpendicular rate when the vessel is already turning, else from
        /// <paramref name="fromToAxis"/> for a from-rest flip; either way a plane is held rather than
        /// tracking the precessing live axis. The commanded axis is rebuilt as nose × tangent from a
        /// blend of the geodesic tangent (toward the target) and the latched-plane tangent, weighted
        /// fully to the latched plane within <see cref="AntipodeHoldAngle"/> (the crawl/pump region)
        /// and smoothstep-blended to pure geodesic by <see cref="AntipodeBlendAngle"/>, so the
        /// hand-back is continuous and ordinary slews are untouched.
        /// </remarks>
        Vector3d ResolveAntipodalAxis (Vector3d fromToAxis, double angleDeg,
            Vector3d currentDirection, Vector3d targetDirection)
        {
            var fromAntipode = 180.0 - angleDeg;
            if (fromAntipode >= AntipodeBlendAngle) {
                antipodeLatched = false;                 // left the band: drop the latch
                return fromToAxis;
            }

            // Latch the flip-plane normal the first time we enter the band. Once latched it is held
            // (never re-read from the live rate) until the error leaves the band. If the vessel is
            // already committed to a rotation, latch its plane (from the perpendicular rate); a
            // from-rest flip has no committed plane, so latch the arbitrary-but-consistent geodesic
            // axis instead — either way the flip then rides a fixed plane, not the precessing live axis.
            if (!antipodeLatched) {
                var worldOmega = vessel.InternalVessel.GetComponent<Rigidbody> ().angularVelocity;
                Vector3d omegaAp = ReferenceFrame.AngularVelocityFromWorldSpace (worldOmega);
                var omegaPerp = omegaAp - Vector3d.Dot (omegaAp, currentDirection) * currentDirection;
                antipodeLatchedNormal = omegaPerp.magnitude >= AntipodeLeadRate
                    ? omegaPerp.normalized
                    : fromToAxis;
                antipodeLatched = true;
            }

            // Latched-plane weight: fully held (1) within AntipodeHoldAngle of antipodal — spanning
            // the crawl/pump region — then smoothstep down to 0 (pure live geodesic) by
            // AntipodeBlendAngle, so the hand-back to normal tracking is continuous.
            double wAngle;
            if (fromAntipode <= AntipodeHoldAngle) {
                wAngle = 1.0;
            } else {
                var t = (AntipodeBlendAngle - fromAntipode) / (AntipodeBlendAngle - AntipodeHoldAngle);
                wAngle = t * t * (3.0 - 2.0 * t);
            }

            // Tangents perpendicular to the nose: toward the target (the geodesic) and forward in the
            // latched plane. nose × tangent rebuilds the rotation axis; at wAngle = 0 (band edge) this
            // is exactly fromToAxis, at wAngle = 1 (the held region) it is the fixed latched plane.
            var tTarget = targetDirection - Vector3d.Dot (targetDirection, currentDirection) * currentDirection;
            var tLatched = Vector3d.Cross (antipodeLatchedNormal, currentDirection);
            var tTargetDir = tTarget.magnitude > 1e-7 ? tTarget.normalized : Vector3d.zero;
            var tLatchedDir = tLatched.magnitude > 1e-7 ? tLatched.normalized : Vector3d.zero;
            var tBlend = (1.0 - wAngle) * tTargetDir + wAngle * tLatchedDir;
            if (tBlend.magnitude <= 1e-7)
                return fromToAxis;
            return Vector3d.Cross (currentDirection, tBlend).normalized;
        }

        /// <summary>
        /// Compute target angular velocity in the roll-invariant frame (pitch,roll,yaw axes with
        /// vessel roll stripped out). cosPhi/sinPhi are cos/sin of the current vessel roll angle.
        /// </summary>
        Vector3d ComputeTargetAngularVelocity (Vector3d torque, Vector3d moi, Vector3d currentOmega,
            Vector3d currentDirection, double cosPhi, double sinPhi)
        {
            var internalVessel = vessel.InternalVessel;
            var targetDirection = EffectiveTargetDirection;

            // Direction error: FromToRotation gives a minimum-arc rotation whose axis is
            // perpendicular to both currentDirection and targetDirection. Because the vessel's
            // nose IS currentDirection (body y-axis), this rotation axis has no y-component in
            // body frame — it carries pure pitch/yaw, no roll.
            QuaternionD dirRotation = GeometryExtensions.FromToRotation (currentDirection, targetDirection);

            double angle;
            Vector3d axis;
            GeometryExtensions.ToAngleAxis (dirRotation, out angle, out axis);
            angle = GeometryExtensions.ClampAngle180 (angle);

            // Resolve the error axis through the 180° (antipodal) singularity, where FromToRotation's
            // axis is arbitrary: if the vessel is already rotating, continue in that plane.
            axis = ResolveAntipodalAxis (axis, angle, currentDirection, targetDirection);

            // Transform direction error from AP frame to body frame, then to roll-invariant frame.
            // The y-component is ~0 by construction (direction error ⊥ nose) and is forced to zero
            // so only x and z carry pitch/yaw.
            var dirAnglesBody = ApToBody (axis * angle);
            var anglesRI = ToRollInvariant (dirAnglesBody, cosPhi, sinPhi);
            anglesRI.y = 0;

            // Roll error: computed separately from the roll residual after direction alignment,
            // projected onto the body y-axis (nose = roll axis). Mixing roll residual into the
            // direction-error vector contaminates pitch/yaw because the residual axis is not
            // aligned with the nose when direction error is large — causing a curved path and
            // roll oscillations. Projecting onto the y-axis extracts the pure roll component.
            if (rollControlled) {
                var dirError = Vector3d.Angle (currentDirection, targetDirection);
                var rollWeight = RollWeight (dirError);
                if (rollWeight > 0) {
                    var currentRotation = ReferenceFrame.RotationFromWorldSpace (internalVessel.ReferenceTransform.rotation);
                    QuaternionD rollResidual = effectiveRotation * currentRotation.Inverse () * dirRotation.Inverse ();
                    double rollResAngle;
                    Vector3d rollResAxis;
                    GeometryExtensions.ToAngleAxis (rollResidual, out rollResAngle, out rollResAxis);
                    if (!double.IsInfinity (rollResAxis.magnitude)) {
                        rollResAngle = GeometryExtensions.ClampAngle180 (rollResAngle);
                        // Project the residual axis onto body y (nose). The y-axis is unchanged
                        // by R_y(phi), so this projection is the same in body and RI frames.
                        var rollResAxisBody = ApToBody (rollResAxis);
                        anglesRI.y = rollResAngle * rollResAxisBody.y * rollWeight;
                    }
                }
            }

            logAngles = anglesRI;

            // Rotate currentOmega into roll-invariant frame for stopping-distance feedforward.
            var currentOmegaRi = ToRollInvariant (currentOmega, cosPhi, sinPhi);

            var result = Vector3d.zero;

            // Roll: 1D per-axis (y-axis is unchanged by the roll-invariant rotation).
            var rollBandwidth = moi [1] > 0 ? RollPID.Kp * torque [1] / moi [1] : 0.0;
            result.y = ComputeAxisVelocity (anglesRI.y, torque [1], moi [1], currentOmegaRi.y,
                MaxAngularVelocity [1], RollAttenuationAngle, rollBandwidth);

            // Pitch/yaw handled jointly so the nose follows a straight great-circle arc.
            double pitchVelocity, yawVelocity;
            ComputePitchYawVelocity (anglesRI, currentOmegaRi, torque, moi, out pitchVelocity, out yawVelocity);
            result.x = pitchVelocity;
            result.z = yawVelocity;

            return result;
        }

        /// <summary>
        /// Compute the joint pitch/yaw target angular velocity in the 2D roll-invariant xz-plane:
        /// aim the velocity profile at the predicted stopping point.
        /// </summary>
        /// <remarks>
        /// Predict where the nose coasts to if the current angular velocity is braked at full
        /// authority — the stopping point as a *vector* — and command the profile straight at it:
        /// <code>
        ///   e_stop = θ + coeff·ω          (coeff·ω is the full-authority stopping displacement)
        ///   ŝ      = e_stop / |e_stop|
        ///   ω_ref  = -ŝ · speed(|e_stop|)
        /// </code>
        /// Tangential damping emerges from the geometry: a sideways drift gives <c>coeff·ω</c> an
        /// off-axis component, tilting <c>e_stop</c> and rotating <c>ŝ</c> so the command acquires a
        /// component opposing the drift. No separate <c>-ω⊥</c> term is needed — the controller leads
        /// the turn to place the predicted stopping point on the target instead of correcting the
        /// orbit after the fact.
        ///
        /// When ω is purely radial (ω⊥ = 0) the command reduces to a pure 1D bang-bang profile along
        /// the error direction: with ω = ω∥·ê, <c>coeff·ω = ½ω∥|ω∥|/α · ê</c>, so
        /// <c>e_stop = θ_ff·ê</c>, <c>ŝ = sign(θ_ff)·ê</c> and great-circle slews and radial settling
        /// are a straight-line speed profile. The off-axis behaviour appears only when ω⊥ ≠ 0 — the
        /// nudge/orbit regime. The stopping displacement is C1 in ω (no radial/tangential seam, no
        /// <c>-ω⊥</c> step), so the acceleration feedforward does not step.
        ///
        /// Anisotropy (design §6, option b): project α (and the PID bandwidth) along ω̂ for the
        /// prediction and along ŝ for the speed profile. Projecting the prediction along ω̂ rather
        /// than ê is what makes <c>e_stop</c> well-defined even at θ = 0, and it coincides with ê
        /// when ω is radial, so the radial reduction above is preserved.
        /// </remarks>
        void ComputePitchYawVelocity (Vector3d anglesRI, Vector3d currentOmegaRi, Vector3d torque,
            Vector3d moi, out double pitchVelocity, out double yawVelocity)
        {
            pitchVelocity = 0;
            yawVelocity = 0;

            var thetaPitch = GeometryExtensions.ToRadians (anglesRI.x);
            var thetaYaw = GeometryExtensions.ToRadians (anglesRI.z);

            var alphaPitch = moi [0] > 0 ? torque [0] / moi [0] : 0.0;
            var alphaYaw = moi [2] > 0 ? torque [2] / moi [2] : 0.0;

            // Stopping-displacement coefficient (>= 0): e_stop = θ + coeff·ω. The quadratic
            // (bang-bang, ω²/2α) and linear (PID-lag, ω/bandwidth) stopping displacements are both
            // collinear with ω, so taking the larger collapses to a scalar max of their coefficients.
            // α and the bandwidth are projected along ω̂ (the brake path is along ω); this coincides
            // with the error-direction projection when ω is radial, preserving the legacy reduction,
            // and stays defined as θ → 0.
            var omegaPitch = currentOmegaRi.x;
            var omegaYaw = currentOmegaRi.z;
            var omegaMag = Math.Sqrt (omegaPitch * omegaPitch + omegaYaw * omegaYaw);

            double coeff = 0;
            if (omegaMag > 0) {
                var oPitch = omegaPitch / omegaMag;
                var oYaw = omegaYaw / omegaMag;
                var alphaOmega = oPitch * oPitch * alphaPitch + oYaw * oYaw * alphaYaw;
                if (alphaOmega > 0) {
                    var coeffQuad = omegaMag / (2.0 * alphaOmega);
                    coeff = coeffQuad;
                    var bw0 = moi [0] > 0 ? PitchPID.Kp * torque [0] / moi [0] : 0.0;
                    var bw2 = moi [2] > 0 ? YawPID.Kp * torque [2] / moi [2] : 0.0;
                    var bandwidthOmega = oPitch * oPitch * bw0 + oYaw * oYaw * bw2;
                    if (bandwidthOmega > 0)
                        coeff = Math.Max (coeffQuad, 1.0 / bandwidthOmega);
                }
            }

            var eStopPitch = thetaPitch + coeff * omegaPitch;
            var eStopYaw = thetaYaw + coeff * omegaYaw;
            var eStopMag = Math.Sqrt (eStopPitch * eStopPitch + eStopYaw * eStopYaw);

            // Singularity guard on |e_stop| (not θ): both the error and the predicted drift must
            // vanish before there is nothing to command. If e_stop ≈ 0 the nose is predicted to
            // coast exactly to the target, so commanding zero is correct.
            if (eStopMag <= MinThetaForJointProfile)
                return;

            var sPitch = eStopPitch / eStopMag;
            var sYaw = eStopYaw / eStopMag;

            // Speed profile along the stopping-point direction ŝ: project α and the max-velocity
            // constraint ellipse along ŝ.
            var alpha2d = sPitch * sPitch * alphaPitch + sYaw * sYaw * alphaYaw;

            var maxVPitch = MaxAngularVelocity [0];
            var maxVYaw = MaxAngularVelocity [2];
            var maxV2d = (maxVPitch > 0 && maxVYaw > 0)
                ? Math.Sqrt (1.0 / (sPitch * sPitch / (maxVPitch * maxVPitch) + sYaw * sYaw / (maxVYaw * maxVYaw)))
                : Math.Min (maxVPitch, maxVYaw);

            double speed = 0;
            if (alpha2d > 0)
                speed = Math.Min (maxV2d, Math.Sqrt (2.0 * eStopMag * alpha2d));

            // Command toward the stopping point, scaled by the linear pointing deadband (see
            // DeadbandScale): the commanded speed fades to zero as the predicted stopping point
            // approaches the target, so the craft coasts to a stop inside the band rather than the inner
            // loop chasing sub-band jitter into the actuators. Applied to the *setpoint* (outer loop), so
            // the inner rate loop keeps its full proportional rate damping and stays well-damped at the
            // hold point. The leading minus defines the controller's positive angular-velocity direction
            // (left-handed sense; matched to the negation in ComputeCurrentAngularVelocity), exactly as
            // the legacy -Math.Sign(θ_ff) did.
            // Key the deadband on the pure pointing error θ, not e_stop: e_stop = θ + coeff·ω carries the
            // measured rate ω, so scaling by it would feed the ±mrad/s rate jitter through the ramp's
            // slope into the setpoint and feedforward. θ is jitter-free, so the deadband edge is quiet.
            var thetaMag = Math.Sqrt (thetaPitch * thetaPitch + thetaYaw * thetaYaw);
            var deadband = DeadbandScale (thetaMag, PitchYawAttenuationAngle);
            pitchVelocity = -sPitch * speed * deadband;
            yawVelocity = -sYaw * speed * deadband;
        }

        /// <summary>
        /// Linear pointing-deadband scale in [0,1] applied to the target angular velocity: 1 at and above
        /// the high angle, ramping linearly to 0 at <see cref="DeadbandLowFraction"/> of it, and 0 below.
        /// Replaces the former logistic attenuation — same role (drive the velocity setpoint to zero as
        /// the error vanishes so the craft coasts to a stop without the inner loop chasing sub-band jitter
        /// into the actuators) but with a linear ramp that reaches exactly zero, giving a clean deadband
        /// rather than a residual tail. <paramref name="errorRad"/> and the returned band are in radians /
        /// degrees respectively.
        /// </summary>
        static double DeadbandScale (double errorRad, double highAngleDeg)
        {
            var high = GeometryExtensions.ToRadians (highAngleDeg);
            var low = DeadbandLowFraction * high;
            if (high <= low)
                return errorRad > low ? 1.0 : 0.0;
            return Math.Min (1.0, Math.Max (0.0, (errorRad - low) / (high - low)));
        }

        /// <summary>
        /// Compute target angular velocity for a single axis using the bang-bang profile.
        /// </summary>
        double ComputeAxisVelocity (double angleDeg, double torque, double moi, double currentOmega,
            double maxVelocity, double deadbandHighDeg, double pidBandwidth = 0.0)
        {
            var theta = GeometryExtensions.ToRadians (angleDeg);
            var thetaPure = theta;   // pure error, before the ω-dependent stopping correction below
            var maxAcceleration = moi > 0 ? torque / moi : 0.0;
            if (maxAcceleration > 0) {
                var corrQuad = 0.5 * currentOmega * Math.Abs (currentOmega) / maxAcceleration;
                var corr = corrQuad;
                if (pidBandwidth > 0) {
                    var corrLinear = currentOmega / pidBandwidth;
                    corr = Math.Abs (corrQuad) >= Math.Abs (corrLinear) ? corrQuad : corrLinear;
                }
                theta += corr;
            }
            // The leading -Math.Sign defines the controller's positive angular-velocity direction as
            // the left-handed sense about the axis; it is the matched partner of the negation in
            // ComputeCurrentAngularVelocity. Flip one without the other and the PI loop diverges.
            // Linear pointing deadband on the target velocity (see DeadbandScale), keyed on the pure error
            // thetaPure (not the ω-corrected theta) so the measured-rate jitter is not fed through the
            // ramp slope: the commanded speed fades to zero as the error approaches the target, so the
            // craft coasts to a stop inside the band. Applied to the setpoint, so the inner rate loop keeps
            // full damping.
            return -Math.Sign (theta) * Math.Min (maxVelocity,
                maxAcceleration > 0 ? Math.Sqrt (2.0 * Math.Abs (theta) * maxAcceleration) : 0.0)
                * DeadbandScale (Math.Abs (thetaPure), deadbandHighDeg);
        }

        void DoAutoTune (Vector3d torque, Vector3d moi)
        {
            DoAutoTuneAxis (PitchPID, 0, torque, moi);
            DoAutoTuneAxis (RollPID, 1, torque, moi);
            DoAutoTuneAxis (YawPID, 2, torque, moi);
        }

        void DoAutoTuneAxis (PIDController pid, int index, Vector3d torque, Vector3d moi)
        {
            if (torque [index] <= 0)
                return;
            var accelerationInv = moi [index] / torque [index];

            // Latched bandwidth reduction: a latched (flexible) axis has its rate-loop bandwidth
            // (2ζω₀ = twiceZetaOmega, since Kp·α = 2ζω₀) pulled down toward oscillationBandwidthFloor,
            // the primary gain-stabiliser — dropping the crossover well below every structural mode so
            // the loop cannot drive any of them. It only ever lowers bandwidth (min against the
            // autotuned value). It is gated on the hold-gated mitigationLevel (latch · holdFactor), so
            // the axis is floored only while *holding* and runs at full bandwidth while *slewing* —
            // the notch/low-pass keeps the slew responsive, while the floor quiets the hold. ω₀ scales
            // with the bandwidth, so Kp scales linearly and Ki quadratically and the damping ratio ζ
            // (the overshoot target) is preserved. A rigid craft never latches, so keeps full bandwidth.
            var level = mitigationLevel [index];
            var bandwidth = twiceZetaOmega [index];
            var bandwidthSquared = omegaSquared [index];
            if (level > 0 && oscillationBandwidthFloor < bandwidth) {
                var reduced = bandwidth + level * (oscillationBandwidthFloor - bandwidth);
                var f = reduced / bandwidth;
                bandwidth = reduced;
                bandwidthSquared *= f * f;
            }

            var kp = bandwidth * accelerationInv;
            var ki = bandwidthSquared * accelerationInv;
            pid.SetParameters (kp, ki, 0, -1, 1);
        }
    }
}
