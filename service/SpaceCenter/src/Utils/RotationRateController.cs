using System;
using System.Diagnostics;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter.Utils
{
    /// <summary>
    /// Controller that aims to get the vessel to rotate with a target angular velocity.
    /// </summary>
    class RotationRateController
    {
        readonly KRPC.SpaceCenter.Services.Vessel vessel;
        public readonly PIDController PitchPID = new PIDController (0);
        public readonly PIDController RollPID = new PIDController (0);
        public readonly PIDController YawPID = new PIDController (0);
        readonly Stopwatch timer = new Stopwatch ();

        public RotationRateController (Vessel vessel, ReferenceFrame referenceFrame)
        {
            this.vessel = new KRPC.SpaceCenter.Services.Vessel (vessel);
            ReferenceFrame = referenceFrame;
            AutoTune = true;
            Overshoot = 0.01f;
            TimeToPeak = 3f;
            Reset ();
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public bool AutoTune { get; set; }

        public float Overshoot { get; set; }

        public float TimeToPeak { get; set; }

        public void Reset ()
        {
            timer.Reset ();
            timer.Start ();
            Target = Vector3d.zero;
            PitchPID.Reset (0);
            YawPID.Reset (0);
            RollPID.Reset (0);
        }

        /// <summary>
        /// Target rotational velocity relative to the coordinate axes of the reference frame.
        /// </summary>
        public Vector3d Target { get; set; }

        public Vector3d Error {
            get {
                //FIXME: finding the rigidbody is expensive - cache it
                return Target + ReferenceFrame.AngularVelocityFromWorldSpace (vessel.InternalVessel.GetComponent<Rigidbody> ().angularVelocity);
            }
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            if (timer.ElapsedMilliseconds < 100)
                return;

            // Convert angular velocities from reference frame, to vessel reference frame so it's aligned with the control axes
            var input = vessel.ReferenceFrame.DirectionFromWorldSpace (ReferenceFrame.DirectionToWorldSpace (Target));
            var error = vessel.ReferenceFrame.DirectionFromWorldSpace (ReferenceFrame.DirectionToWorldSpace (Error));

            var torque = vessel.AvailableTorqueVector;
            var moi = vessel.MomentOfInertiaVector;

            // Apply low-pass filter to target angular velocity, so required acceleration is within the capabilities of the vessel

            if (AutoTune)
                // TODO: only run this once every fixed time period to improve performance?
                DoAutoTune (torque, moi);

            // If vessel is not controllable in an axis, zero out the integral term

            // If vessel is sat on the pad, zero out the integral terms
            if (vessel.InternalVessel.situation == Vessel.Situations.PRELAUNCH) {
                PitchPID.ClearIntegralTerm ();
                RollPID.ClearIntegralTerm ();
                YawPID.ClearIntegralTerm ();
            }

            // Run per-axis PID controllers
            var output = new Vector3d (
                             PitchPID.Update (error.x, input.x),
                             RollPID.Update (error.y, input.y),
                             YawPID.Update (error.z, input.z));

            // Set control inputs
            state.Pitch = (float)output.x;
            state.Roll = (float)output.y;
            state.Yaw = (float)output.z;

            timer.Reset ();
            timer.Start ();
        }

        void DoAutoTune (Vector3d torque, Vector3d moi)
        {
            //TODO: cache these values to improve performance
            var logOvershoot = Math.Log (Overshoot);
            var sqLogOvershoot = logOvershoot * logOvershoot;
            var z = Math.Sqrt (sqLogOvershoot / (Math.PI * Math.PI + sqLogOvershoot));
            var w = Math.PI / (TimeToPeak * Math.Sqrt (1.0 - z * z));
            DoAutoTuneAxis (PitchPID, torque.x, moi.x, z, w);
            DoAutoTuneAxis (RollPID, torque.y, moi.y, z, w);
            DoAutoTuneAxis (YawPID, torque.z, moi.z, z, w);
        }

        static void DoAutoTuneAxis (PIDController pid, double torque, double moi, double z, double w)
        {
            // Don't tune PID for axis when the torque is small
            if (torque < 1)
                return;
            //TODO: cache these values to improve performance
            var yInv = moi / torque;
            var kp = 2 * z * w * yInv;
            var ki = w * w * yInv;
            pid.SetParameters (kp, ki, 0, -1, 1);
        }
    }
}
