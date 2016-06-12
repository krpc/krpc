using System;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.SpaceCenter.Utils
{
    /// <summary>
    /// Controller that aims to get the vessel to rotate with a target rotational velocity.
    /// </summary>
    class RotationRateController
    {
        readonly KRPC.SpaceCenter.Services.Vessel vessel;
        public readonly PIDController PitchPID = new PIDController (0, 0);
        public readonly PIDController RollPID = new PIDController (0, 0);
        public readonly PIDController YawPID = new PIDController (0, 0);

        public RotationRateController (Vessel vessel)
        {
            this.vessel = new KRPC.SpaceCenter.Services.Vessel (vessel);
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public Vector3 Target { get; set; }

        public void AutoTune (double overshoot, double timeToPeak)
        {
            var torque = vessel.AvailableTorqueVector;
            var moi = vessel.MomentOfInertiaVector;

            var logOvershoot = Math.Log (overshoot);
            var sqLogOvershoot = logOvershoot * logOvershoot;
            var z = Math.Sqrt (sqLogOvershoot / (Math.PI * Math.PI + sqLogOvershoot));
            var w = Math.PI / (timeToPeak * Math.Sqrt (1.0 - z * z));
            var y = new Vector3d (moi.x / torque.x, moi.y / torque.y, moi.z / torque.z);
            var kp = 2 * z * w * y;
            var ki = w * w * y;

            PitchPID.SetParameters (kp.x, ki.x, 0, -1, 1);
            RollPID.SetParameters (kp.y, ki.y, 0, -1, 1);
            YawPID.SetParameters (kp.z, ki.z, 0, -1, 1);
        }

        public Vector3 Error {
            get {
                //FIXME: finding the rigidbody is expensive - cache it
                return Target + ReferenceFrame.AngularVelocityFromWorldSpace (vessel.InternalVessel.GetComponent<Rigidbody> ().angularVelocity);
            }
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            var error = Error;
            var output = new Vector3 (
                             (float)PitchPID.Update (error.x, Target.x),
                             (float)RollPID.Update (error.y, Target.y),
                             (float)YawPID.Update (error.z, Target.z));

            var pitchAxis = ReferenceFrame.DirectionFromWorldSpace (vessel.ReferenceFrame.DirectionToWorldSpace (Vector3.right));
            var rollAxis = ReferenceFrame.DirectionFromWorldSpace (vessel.InternalVessel.ReferenceTransform.up);
            var yawAxis = ReferenceFrame.DirectionFromWorldSpace (vessel.ReferenceFrame.DirectionToWorldSpace (Vector3.forward));

            state.Pitch = Vector3.Dot (output, pitchAxis);
            state.Roll = Vector3.Dot (output, rollAxis);
            state.Yaw = Vector3.Dot (output, yawAxis);
        }
    }
}
