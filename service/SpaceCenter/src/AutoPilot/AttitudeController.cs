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
        public readonly PIDController PitchPID = new PIDController (0);
        public readonly PIDController RollPID = new PIDController (0);
        public readonly PIDController YawPID = new PIDController (0);

        // Target orientation — quaternion is the single source of truth.
        // rollControlled=false means "suppress roll rotation" (don't hold a specific angle).
        QuaternionD targetRotation;
        bool rollControlled;

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
        Vector3d logAngles;
        bool diagnosticLogging;
        readonly StringBuilder diagnosticLog = new StringBuilder ();
        readonly object diagnosticLogLock = new object ();

        // Time constant for the one-sided torque smoothing (see UpdateSmoothedTorque).
        const double TorqueSmoothTimeConstant = 0.5;
        // Time constant for the acceleration-feedforward low-pass filter (a few physics ticks).
        // Long enough to attenuate the single-tick steps the bang-bang profile's slope
        // discontinuities produce, short enough that the feedforward lag stays negligible.
        const double FeedforwardSmoothTimeConstant = 0.05;
        // Slope factor of the sigmoid that attenuates the velocity setpoint near the target.
        const double AttenuationSigmoidSlope = 6.0;
        // Below this 2D error magnitude (radians) the joint pitch/yaw profile is skipped.
        const double MinThetaForJointProfile = 1e-10;

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

        void SetTarget (double pitch, double heading, double roll)
        {
            rollControlled = !double.IsNaN (roll);
            var phr = new Vector3d (pitch, heading, rollControlled ? roll : 0);
            targetRotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (phr);
        }

        public void SetTargetDirection (Vector3d direction)
        {
            targetRotation = GeometryExtensions.FromToRotation (Vector3d.up, direction.normalized);
            rollControlled = false;
        }

        public void SetTargetRotation (QuaternionD rotation)
        {
            targetRotation = rotation;
            rollControlled = true;
        }

        public Vector3d MaxAngularVelocity { get; set; }

        public Vector3d AttenuationAngle { get; set; }

        public double RollStartAngle { get; set; }

        public double RollEngageAngle { get; set; }

        public bool AutoTune { get; set; }

        public bool DecelLagCorrection { get; set; }

        public bool GyroscopicCompensation { get; set; }

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
            AttenuationAngle = new Vector3d (1, 1, 1);
            RollStartAngle = 20.0;
            RollEngageAngle = 15.0;
            AutoTune = true;
            Overshoot = new Vector3d (0.01, 0.01, 0.01);
            // TimeToPeak sets the inner-loop bandwidth via omega0 = pi / (TimeToPeak * sqrt(1 - zeta^2)).
            // Increasing it lowers the bandwidth, which is the lever for large, structurally flexible
            // vehicles: when the bandwidth approaches the structural resonance frequency (e.g. the
            // Ariane rocket, ~10 rad/s) the PID saturates in response to structural angular velocity
            // oscillations and drives the bending mode. Such craft need a larger TimeToPeak and/or
            // DecelLagCorrection=false.
            TimeToPeak = new Vector3d (1, 1, 1);
            DecelLagCorrection = true;
            GyroscopicCompensation = true;
            DiagnosticLogging = false;
            SetTarget (0, 0, double.NaN);
            Start ();
        }

        public void Start ()
        {
            PitchPID.Reset (0);
            RollPID.Reset (0);
            YawPID.Reset (0);
            prevTargetRiValid = false;
            smoothedFfRi = Vector3d.zero;
            if (AutoTune)
                DoAutoTune (vessel.AvailableTorqueVectors.Item1, vessel.MomentOfInertiaVector);
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            var internalVessel = vessel.InternalVessel;
            var torque = vessel.AvailableTorqueVectors.Item1;
            var moi = vessel.MomentOfInertiaVector;
            var dt = Time.fixedDeltaTime;

            // Compute the roll-invariant frame: a frame sharing the vessel's nose direction but
            // with zero roll relative to the AP reference frame. Expressing both the target and
            // current angular velocities in this frame means roll corrections do not disturb the
            // path taken to point the vessel.
            Vector3d currentDirection;
            double phi, cosPhi, sinPhi;
            ComputeRollInvariantFrame (internalVessel, out currentDirection, out phi, out cosPhi, out sinPhi);

            // Current and target angular velocities, both expressed in the roll-invariant frame.
            var current = ComputeCurrentAngularVelocity ();
            var currentRi = ToRollInvariant (current, cosPhi, sinPhi);
            var target = ComputeTargetAngularVelocity (torque, moi, current, currentDirection, cosPhi, sinPhi);

            // Roll setpoint is already weighted to zero inside ComputeTargetAngularVelocity when the
            // vessel is far from the direction target; clear the integral there to prevent windup.
            if (!rollControlled)
                target.y = 0;
            else
                ClearRollWindupIfDisengaged (currentDirection);

            // Acceleration feedforward: differentiate the velocity setpoint numerically to get the
            // angular acceleration needed to stay on the bang-bang trajectory, then normalise by
            // α_max = torque/moi so the feedforward is a control fraction in [-1, 1].
            // Skipped on the first tick (prevTargetRiValid == false) to avoid a spike.
            var rawFfRi = Vector3d.zero;
            if (prevTargetRiValid) {
                var alphaPitch = moi [0] > 0 ? torque [0] / moi [0] : 0.0;
                var alphaRoll  = moi [1] > 0 ? torque [1] / moi [1] : 0.0;
                var alphaYaw   = moi [2] > 0 ? torque [2] / moi [2] : 0.0;
                if (alphaPitch > 0) rawFfRi.x = (target.x - prevTargetRi.x) / (dt * alphaPitch);
                if (alphaRoll  > 0) rawFfRi.y = (target.y - prevTargetRi.y) / (dt * alphaRoll);
                if (alphaYaw   > 0) rawFfRi.z = (target.z - prevTargetRi.z) / (dt * alphaYaw);
            }
            prevTargetRi = target;
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
            var ffRi = smoothedFfRi;

            UpdateSmoothedTorque (torque);

            // Autotune the controllers if enabled (uses smoothed torque to avoid gain spikes)
            if (AutoTune)
                DoAutoTune (smoothedTorque, moi);

            // If vessel is sat on the pad, zero out the integral terms
            if (internalVessel.situation == Vessel.Situations.PRELAUNCH) {
                PitchPID.ClearIntegralTerm ();
                RollPID.ClearIntegralTerm ();
                YawPID.ClearIntegralTerm ();
            }

            // Run pitch/yaw PIDs in the roll-invariant frame; roll PID on the y-axis (unchanged by
            // the frame rotation). Add the acceleration feedforward to each PID output, then clamp
            // before converting pitch/yaw back to the body frame.
            var virtualPitch = (RunAxis (PitchPID, target.x, currentRi.x, torque [0], dt) + ffRi.x).Clamp (-1, 1);
            var virtualRoll = (RunAxis (RollPID, target.y, currentRi.y, torque [1], dt) + ffRi.y).Clamp (-1, 1);
            var virtualYaw = (RunAxis (YawPID, target.z, currentRi.z, torque [2], dt) + ffRi.z).Clamp (-1, 1);
            var bodyControl = FromRollInvariant (new Vector3d (virtualPitch, 0, virtualYaw), cosPhi, sinPhi);

            // Gyroscopic feedforward: the per-axis plant model (τ = I·ω̇) ignores the ω×(Iω) term in
            // Euler's rigid-body equation. Add a control fraction that cancels it, in the body frame
            // where the inertia and available torque are per-axis, then sum with the control and
            // clamp to [-1, 1].
            var gyro = GyroscopicFeedforward (current, moi, torque);
            state.Pitch = (float)(bodyControl.x + gyro.x).Clamp (-1, 1);
            state.Roll = (float)(virtualRoll + gyro.y).Clamp (-1, 1);
            state.Yaw = (float)(bodyControl.z + gyro.z).Clamp (-1, 1);

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
            var Q_point_ap = GeometryExtensions.FromToRotation (Vector3d.up, currentDirection);
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
            var dirError = Vector3.Angle (currentDirection, TargetDirection);
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
            var dirErr = Vector3.Angle (currentDirection, TargetDirection);
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
                " ctrl=(p={33:F3},r={34:F3},y={35:F3})",
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
                state.Pitch, state.Roll, state.Yaw);
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
        /// Gyroscopic feedforward in the body frame, returned as a per-axis control fraction.
        /// </summary>
        /// <remarks>
        /// The PID controllers and the autotuner model the plant as τ = I·ω̇ independently per axis,
        /// but the rigid-body equation of motion is τ = I·ω̇ + ω×(Iω). The cross term ω×(Iω) is a
        /// torque the controller would otherwise have to reject as a disturbance. This returns the
        /// control fraction that cancels it: -(ω×(Iω))ᵢ / τ_max,ᵢ per axis (a diagonal inertia is
        /// assumed, matching the rest of the controller). The term is quadratic in ω, so it is
        /// negligible at the low rates of normal attitude holding — including structural bending
        /// oscillation — and only matters for fast slews or strongly asymmetric inertia. It can be
        /// disabled via <see cref="GyroscopicCompensation"/>.
        ///
        /// ω is passed in the controller's negated sign convention (see ComputeCurrentAngularVelocity),
        /// but ω×(Iω) is quadratic in ω and so is invariant under that negation — it gives the correct
        /// body-frame gyroscopic torque either way.
        /// </remarks>
        Vector3d GyroscopicFeedforward (Vector3d omega, Vector3d moi, Vector3d torque)
        {
            if (!GyroscopicCompensation)
                return Vector3d.zero;
            var angularMomentum = new Vector3d (moi.x * omega.x, moi.y * omega.y, moi.z * omega.z);
            var gyroTorque = Vector3d.Cross (omega, angularMomentum);
            return new Vector3d (
                torque.x > 0 ? -gyroTorque.x / torque.x : 0.0,
                torque.y > 0 ? -gyroTorque.y / torque.y : 0.0,
                torque.z > 0 ? -gyroTorque.z / torque.z : 0.0);
        }

        /// <summary>
        /// Compute target angular velocity in the roll-invariant frame (pitch,roll,yaw axes with
        /// vessel roll stripped out). cosPhi/sinPhi are cos/sin of the current vessel roll angle.
        /// </summary>
        Vector3d ComputeTargetAngularVelocity (Vector3d torque, Vector3d moi, Vector3d currentOmega,
            Vector3d currentDirection, double cosPhi, double sinPhi)
        {
            var internalVessel = vessel.InternalVessel;
            var targetDirection = TargetDirection;

            // Direction error: FromToRotation gives a minimum-arc rotation whose axis is
            // perpendicular to both currentDirection and targetDirection. Because the vessel's
            // nose IS currentDirection (body y-axis), this rotation axis has no y-component in
            // body frame — it carries pure pitch/yaw, no roll.
            QuaternionD dirRotation = GeometryExtensions.FromToRotation (currentDirection, targetDirection);

            double angle;
            Vector3d axis;
            GeometryExtensions.ToAngleAxis (dirRotation, out angle, out axis);
            angle = GeometryExtensions.ClampAngle180 (angle);

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
                    QuaternionD rollResidual = targetRotation * currentRotation.Inverse () * dirRotation.Inverse ();
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
            var rollBandwidth = DecelLagCorrection && moi [1] > 0 ? RollPID.Kp * torque [1] / moi [1] : 0.0;
            result.y = ComputeAxisVelocity (anglesRI.y, torque [1], moi [1], currentOmegaRi.y,
                MaxAngularVelocity [1], AttenuationAngle [1], rollBandwidth);

            // Pitch/yaw handled jointly so the nose follows a straight great-circle arc.
            double pitchVelocity, yawVelocity;
            ComputePitchYawVelocity (anglesRI, currentOmegaRi, torque, moi, out pitchVelocity, out yawVelocity);
            result.x = pitchVelocity;
            result.z = yawVelocity;

            return result;
        }

        /// <summary>
        /// Compute the joint pitch/yaw target angular velocity in the 2D roll-invariant xz-plane.
        /// </summary>
        /// <remarks>
        /// Per-axis bang-bang gives ω ∝ sqrt(|θ|) per component, so the velocity direction differs
        /// from the error direction and the path curves. Treating pitch/yaw jointly applies the
        /// profile once to the total 2D error magnitude, then scales the result back along the unit
        /// error direction, giving ω ∝ |θ|_total in the error direction and a straight path.
        /// </remarks>
        void ComputePitchYawVelocity (Vector3d anglesRI, Vector3d currentOmegaRi, Vector3d torque,
            Vector3d moi, out double pitchVelocity, out double yawVelocity)
        {
            pitchVelocity = 0;
            yawVelocity = 0;

            var thetaPitch = GeometryExtensions.ToRadians (anglesRI.x);
            var thetaYaw = GeometryExtensions.ToRadians (anglesRI.z);
            var theta2d = Math.Sqrt (thetaPitch * thetaPitch + thetaYaw * thetaYaw);
            if (theta2d <= MinThetaForJointProfile)
                return;

            var dirPitch = thetaPitch / theta2d;
            var dirYaw = thetaYaw / theta2d;

            // Acceleration projected along the path direction
            var alphaPitch = moi [0] > 0 ? torque [0] / moi [0] : 0.0;
            var alphaYaw = moi [2] > 0 ? torque [2] / moi [2] : 0.0;
            var alpha2d = dirPitch * dirPitch * alphaPitch + dirYaw * dirYaw * alphaYaw;

            // Current omega projected along path for stopping-distance feedforward.
            // corrQuad (ω²/2α) is the bang-bang stopping distance: always used.
            // corrLinear (ω/bandwidth) is the PID-lag stopping distance: valid when the PID is
            // unsaturated and decelerating more slowly than full torque. It is the larger
            // correction in that regime and prevents overshoot on rigid craft. However, it
            // amplifies structural bending-mode noise on flexible rockets — set DecelLagCorrection
            // to false to suppress it and use corrQuad only.
            var omega2d = currentOmegaRi.x * dirPitch + currentOmegaRi.z * dirYaw;

            var theta2dFf = theta2d;
            if (alpha2d > 0) {
                var corrQuad = 0.5 * omega2d * Math.Abs (omega2d) / alpha2d;
                var corr = corrQuad;
                if (DecelLagCorrection) {
                    var bw0 = moi [0] > 0 ? PitchPID.Kp * torque [0] / moi [0] : 0.0;
                    var bw2 = moi [2] > 0 ? YawPID.Kp * torque [2] / moi [2] : 0.0;
                    var bandwidth2d = dirPitch * dirPitch * bw0 + dirYaw * dirYaw * bw2;
                    if (bandwidth2d > 0) {
                        var corrLinear = omega2d / bandwidth2d;
                        corr = Math.Abs (corrQuad) >= Math.Abs (corrLinear) ? corrQuad : corrLinear;
                    }
                }
                theta2dFf += corr;
            }

            // Maximum 2D velocity: constraint ellipse radius in the direction (dirPitch, dirYaw)
            var maxVPitch = MaxAngularVelocity [0];
            var maxVYaw = MaxAngularVelocity [2];
            var maxV2d = (maxVPitch > 0 && maxVYaw > 0)
                ? Math.Sqrt (1.0 / (dirPitch * dirPitch / (maxVPitch * maxVPitch) + dirYaw * dirYaw / (maxVYaw * maxVYaw)))
                : Math.Min (maxVPitch, maxVYaw);

            double velocity2d = 0;
            if (alpha2d > 0)
                velocity2d = -Math.Sign (theta2dFf) * Math.Min (maxV2d, Math.Sqrt (2.0 * Math.Abs (theta2dFf) * alpha2d));

            var attAngle2d = GeometryExtensions.ToRadians (Math.Min (AttenuationAngle [0], AttenuationAngle [2]));
            var attenuation2d = 1.0 / (1.0 + Math.Exp (-((Math.Abs (theta2dFf) - attAngle2d) * (AttenuationSigmoidSlope / attAngle2d))));
            if (double.IsNaN (attenuation2d))
                attenuation2d = 0;

            pitchVelocity = velocity2d * attenuation2d * dirPitch;
            yawVelocity = velocity2d * attenuation2d * dirYaw;

            // Damp the perpendicular (tangential) component of the current angular velocity.
            // The outer-loop setpoint above is purely radial (toward the target direction).
            // When the vessel has tangential angular velocity — e.g. after a nudge that puts it
            // into a circular orbit around the target — the radial setpoint provides centripetal
            // force but no tangential braking.  That allows a self-sustaining limit cycle: the
            // controller pulls the nose toward the target while the nose circles around it at
            // constant radius.  Subtracting the perpendicular component of the current omega from
            // the setpoint makes the inner PID cancel the tangential motion directly, breaking the
            // orbit.  During a normal radial approach omega_perp ≈ 0, so this term is dormant.
            pitchVelocity -= currentOmegaRi.x - omega2d * dirPitch;
            yawVelocity -= currentOmegaRi.z - omega2d * dirYaw;
        }

        /// <summary>
        /// Compute target angular velocity for a single axis using the bang-bang profile.
        /// </summary>
        double ComputeAxisVelocity (double angleDeg, double torque, double moi, double currentOmega,
            double maxVelocity, double attenuationAngleDeg, double pidBandwidth = 0.0)
        {
            var theta = GeometryExtensions.ToRadians (angleDeg);
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
            var velocity = -Math.Sign (theta) * Math.Min (maxVelocity,
                maxAcceleration > 0 ? Math.Sqrt (2.0 * Math.Abs (theta) * maxAcceleration) : 0.0);
            var attAngle = GeometryExtensions.ToRadians (attenuationAngleDeg);
            var attenuation = 1.0 / (1.0 + Math.Exp (-((Math.Abs (theta) - attAngle) * (AttenuationSigmoidSlope / attAngle))));
            if (double.IsNaN (attenuation))
                attenuation = 0;
            return velocity * attenuation;
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
            var kp = twiceZetaOmega [index] * accelerationInv;
            var ki = omegaSquared [index] * accelerationInv;
            pid.SetParameters (kp, ki, 0, -1, 1);
        }
    }
}
