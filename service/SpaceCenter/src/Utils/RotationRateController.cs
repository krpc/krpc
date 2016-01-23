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
        global::Vessel vessel;
        public readonly PIDController pid = new PIDController ();

        public RotationRateController (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public Vector3 Target { get; set; }

        public Vector3 Error {
            get {
                var velocity = ReferenceFrame.AngularVelocityFromWorldSpace (-vessel.rigidbody.angularVelocity);
                var error = Target - velocity;
                var pitchAxis = ReferenceFrame.DirectionFromWorldSpace (ReferenceFrame.Object (vessel).DirectionToWorldSpace (Vector3.right));
                var yawAxis = ReferenceFrame.DirectionFromWorldSpace (ReferenceFrame.Object (vessel).DirectionToWorldSpace (Vector3.forward));
                var rollAxis = ReferenceFrame.DirectionFromWorldSpace (vessel.ReferenceTransform.up);
                var pitch = Vector3.Dot (error, pitchAxis);
                var yaw = Vector3.Dot (error, yawAxis);
                var roll = Vector3.Dot (error, rollAxis);
                return new Vector3 (pitch, yaw, roll);
            }
        }

        public void Update (PilotAddon.ControlInputs state)
        {
            var output = pid.Update (Error, Target, -1f, 1f);
            state.Pitch = output.x;
            state.Yaw = output.y;
            state.Roll = output.z;
        }
    }
}
