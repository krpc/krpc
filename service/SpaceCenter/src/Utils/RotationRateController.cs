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
        Guid vesselId;
        public readonly PIDController PID = new PIDController ();

        public RotationRateController (Vessel vessel)
        {
            vesselId = vessel.id;
        }

        public ReferenceFrame ReferenceFrame { get; set; }

        public Vector3 Target { get; set; }

        public Vector3 Error {
            get {
                var vessel = FlightGlobalsExtensions.GetVesselById (vesselId);
                //FIXME: finding the rigidbody is expensive - cache it
                var velocity = ReferenceFrame.AngularVelocityFromWorldSpace (-vessel.GetComponent<Rigidbody> ().angularVelocity);
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
            var output = PID.Update (Error, Target, -1f, 1f);
            state.Pitch = output.x;
            state.Yaw = output.y;
            state.Roll = output.z;
        }
    }
}
