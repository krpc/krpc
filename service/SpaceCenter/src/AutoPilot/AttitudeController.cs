using System;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter.AutoPilot
{
    /// <summary>
    /// Controller to hold a vessels attitude in a chosen orientation.
    /// </summary>
    class AttitudeController
    {
        readonly KRPC.SpaceCenter.Services.Vessel vessel;
        public readonly PIDController PitchPID = new PIDController (0);
        public readonly PIDController RollPID = new PIDController (0);
        public readonly PIDController YawPID = new PIDController (0);
        double targetPitch;
        double targetHeading;
        double targetRoll;
        Vector3d targetDirection;
        QuaternionD targetRotation;
        float overshoot;
        float timeToPeak;
        double twiceZetaOmega;
        double omegaSquared;
        const float timePerUpdate = 0.1f;
        float deltaTime;

        public AttitudeController (Vessel vessel)
        {
            this.vessel = new KRPC.SpaceCenter.Services.Vessel (vessel);
            ReferenceFrame = this.vessel.SurfaceReferenceFrame;
            MaxRotationSpeed = 1f;
            AutoTune = true;
            Overshoot = 0.01f;
            TimeToPeak = 3f;
            Start ();
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public double TargetPitch {
            get { return targetPitch; }
            set {
                targetPitch = value;
                UpdateTarget ();
            }
        }

        public double TargetHeading {
            get { return targetHeading; }
            set {
                targetHeading = value;
                UpdateTarget ();
            }
        }

        public double TargetRoll {
            get { return targetRoll; }
            set {
                targetRoll = value;
                UpdateTarget ();
            }
        }

        public Vector3d TargetDirection {
            get { return targetDirection; }
        }

        public QuaternionD TargetRotation {
            get { return targetRotation; }
        }

        void UpdateTarget ()
        {
            var phr = new Vector3d (targetPitch, targetHeading, double.IsNaN (targetRoll) ? 0 : targetRoll);
            targetRotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (phr);
            targetDirection = targetRotation * Vector3.up;
        }

        public float MaxRotationSpeed { get; set; }

        public bool AutoTune { get; set; }

        public float Overshoot {
            get { return overshoot; }
            set {
                overshoot = value;
                UpdateParameters ();
            }
        }

        public float TimeToPeak {
            get { return timeToPeak; }
            set {
                timeToPeak = value;
                UpdateParameters ();
            }
        }

        void UpdateParameters ()
        {
            var logOvershoot = Math.Log (overshoot);
            var sqLogOvershoot = logOvershoot * logOvershoot;
            var zeta = Math.Sqrt (sqLogOvershoot / (Math.PI * Math.PI + sqLogOvershoot));
            var omega = Math.PI / (timeToPeak * Math.Sqrt (1.0 - zeta * zeta));
            twiceZetaOmega = 2 * zeta * omega;
            omegaSquared = omega * omega;
        }

        public void Start ()
        {
            PitchPID.Reset (0);
            RollPID.Reset (0);
            YawPID.Reset (0);
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            // Run the controller once every timePerUpdate seconds
            deltaTime += Time.fixedDeltaTime;
            if (deltaTime < timePerUpdate)
                return;
            Console.WriteLine ("dT = " + deltaTime);

            // Compute the input and error for the controllers
            var input = ComputeTargetAngularVelocity ();
            var current = -ReferenceFrame.AngularVelocityFromWorldSpace (vessel.InternalVessel.GetComponent<Rigidbody> ().angularVelocity);

            // Convert input and error to reference frame rotated with the control axes
            var rotation = vessel.ReferenceFrame.Rotation.Inverse () * ReferenceFrame.Rotation;
            input = rotation * input;
            current = rotation * current;

            Console.WriteLine ("input = " + input);
            Console.WriteLine ("current = " + current);

            // Autotune the controllers if enabled
            if (AutoTune)
                DoAutoTune ();

            // If vessel is sat on the pad, zero out the integral terms
            if (vessel.InternalVessel.situation == Vessel.Situations.PRELAUNCH) {
                PitchPID.ClearIntegralTerm ();
                RollPID.ClearIntegralTerm ();
                YawPID.ClearIntegralTerm ();
                Console.WriteLine ("prelaunch");

            }

            // Run per-axis PID controllers
            var output = new Vector3d (
                             PitchPID.Update (input.x, current.x, deltaTime),
                             RollPID.Update (input.y, current.y, deltaTime),
                             YawPID.Update (input.z, current.z, deltaTime));
            state.Pitch = (float)output.x;
            state.Roll = (float)output.y;
            state.Yaw = (float)output.z;
            Console.WriteLine ("output = " + output);

            deltaTime = 0;
        }

        Vector3 ComputeTargetAngularVelocity ()
        {
            var currentRotation = ReferenceFrame.RotationFromWorldSpace (vessel.InternalVessel.ReferenceTransform.rotation);
            var currentDirection = ReferenceFrame.DirectionFromWorldSpace (vessel.InternalVessel.ReferenceTransform.up);

            QuaternionD rotation;
            if (Vector3.Angle (currentDirection, targetDirection) < 5 && !double.IsNaN (TargetRoll))
                // Pointing close to target direction and roll angle set
                // Use rotation from currentRotation -> targetRotation
                rotation = targetRotation * currentRotation.Inverse ();
            else
                // Pointing far from target direction or roll angle not set
                // Use rotation from currentDirection -> targetDirection
                //FIXME: QuaternionD.FromToRotation method not available at runtime
                rotation = Quaternion.FromToRotation (currentDirection, targetDirection);

            // Compute angles for the rotation in pitch (x), roll (y), yaw (z) axes
            float angleFloat;
            Vector3 axisFloat;
            //FIXME: QuaternionD.ToAngleAxis method not available at runtime
            ((Quaternion)rotation).ToAngleAxis (out angleFloat, out axisFloat);
            double angle = GeometryExtensions.ClampAngle180 (angleFloat);
            Vector3d axis = axisFloat;
            var angles = axis * angle;
            //TODO: compute max rotation speed based on torque and angular acceleration, per axis
            var rate = (MaxRotationSpeed / 180) * angles;
            return -rate; //FIXME: why negative here???
        }

        void DoAutoTune ()
        {
            var torque = vessel.AvailableTorqueVector;
            var moi = vessel.MomentOfInertiaVector;
            DoAutoTuneAxis (PitchPID, torque.x, moi.x);
            DoAutoTuneAxis (RollPID, torque.y, moi.y);
            DoAutoTuneAxis (YawPID, torque.z, moi.z);
        }

        void DoAutoTuneAxis (PIDController pid, double torque, double moi)
        {
            var accelerationInv = moi / torque;
            // Don't tune when the available acceleration is less than 0.001 radian.s^-2
            if (accelerationInv > 1000)
                return;
            var kp = twiceZetaOmega * accelerationInv;
            var ki = omegaSquared * accelerationInv;
            pid.SetParameters (kp, ki, 0, -1, 1);
        }
    }
}
