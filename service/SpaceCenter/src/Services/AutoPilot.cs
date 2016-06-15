using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Utils;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Provides basic auto-piloting utilities for a vessel.
    /// Created by calling <see cref="Vessel.AutoPilot"/>.
    /// </summary>
    /// <remarks>
    /// If a client engages the auto-pilot and then closes its connection to the server,
    /// the auto-pilot will be disengaged and its target reference frame, direction and roll reset to default.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class AutoPilot : Equatable<AutoPilot>
    {
        static IDictionary<Guid, AutoPilot> engaged = new Dictionary<Guid, AutoPilot> ();
        readonly Guid vesselId;
        readonly RotationRateController rotationRateController;
        IClient requestingClient;
        Vector3d targetPHR;

        internal AutoPilot (global::Vessel vessel)
        {
            if (!engaged.ContainsKey (vessel.id))
                engaged [vessel.id] = null;
            vesselId = vessel.id;
            rotationRateController = new RotationRateController (vessel, ReferenceFrame.Surface (vessel));
            targetPHR = Vector3.zero;
            targetPHR.z = double.NaN;
            MaxRotationSpeed = 1f;
        }

        /// <summary>
        /// Check the auto-pilots are for the same vessel.
        /// </summary>
        public override bool Equals (AutoPilot obj)
        {
            return vesselId == obj.vesselId;
        }

        /// <summary>
        /// Hash the auto-pilot.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get { return FlightGlobalsExtensions.GetVesselById (vesselId); }
        }

        /// <summary>
        /// Engage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Engage ()
        {
            requestingClient = KRPCCore.Context.RPCClient;
            engaged [vesselId] = this;
            rotationRateController.Reset ();
        }

        /// <summary>
        /// Disengage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Disengage ()
        {
            requestingClient = null;
            engaged [vesselId] = null;
        }

        /// <summary>
        /// Blocks until the vessel is pointing in the target direction and has the target roll (if set).
        /// </summary>
        [KRPCMethod]
        public void Wait ()
        {
            if (Error > 0.5f || RollError > 0.5f || InternalVessel.GetComponent<Rigidbody> ().angularVelocity.magnitude > 0.05f)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        /// <summary>
        /// The error, in degrees, between the direction the ship has been asked
        /// to point in and the direction it is pointing in. Returns zero if the auto-pilot
        /// has not been engaged and SAS is not enabled or is in stability assist mode.
        /// </summary>
        [KRPCProperty]
        public float Error {
            get {
                if (engaged [vesselId] == this)
                    return Vector3.Angle (InternalVessel.ReferenceTransform.up, ReferenceFrame.DirectionToWorldSpace (TargetDirectionVector));
                else if (engaged [vesselId] != this && SAS && SASMode != SASMode.StabilityAssist)
                    return Vector3.Angle (InternalVessel.ReferenceTransform.up, SASTargetDirection ());
                else
                    return 0f;
            }
        }

        Vector3d TargetDirectionVector {
            get {
                // set roll to 0 as it could be NaN
                var phr = targetPHR;
                phr.z = 0;
                var targetRotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (phr);
                return targetRotation * Vector3.up;
            }
        }

        /// <summary>
        /// The error, in degrees, between the roll the ship has been asked to be
        /// in and the actual roll. Returns zero if the auto-pilot has not been engaged
        /// or no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float RollError {
            get {
                if (engaged [vesselId] != this || double.IsNaN (targetPHR.z))
                    return 0f;
                var currentRoll = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                return (float)Math.Abs (GeometryExtensions.ClampAngle180 (targetPHR.z - currentRoll));
            }
        }

        /// <summary>
        /// The reference frame for the target direction (<see cref="AutoPilot.TargetDirection"/>).
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return rotationRateController.ReferenceFrame; }
            set { rotationRateController.ReferenceFrame = value; }
        }

        /// <summary>
        /// The target direction.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TargetDirection {
            get { return TargetDirectionVector.ToTuple (); }
            set {
                //FIXME: QuaternionD.FromToRotation method not available at runtime
                var rotation = (QuaternionD)Quaternion.FromToRotation (Vector3d.up, value.ToVector ());
                var phr = rotation.PitchHeadingRoll ();
                targetPHR.x = phr.x;
                targetPHR.y = phr.y;
            }
        }

        /// <summary>
        /// Set (<see cref="AutoPilot.TargetDirection"/>) from a pitch and heading angle.
        /// </summary>
        /// <param name="pitch">Target pitch angle, in degrees between -90° and +90°.</param>
        /// <param name="heading">Target heading angle, in degrees between 0° and 360°.</param>
        [KRPCMethod]
        public void TargetPitchAndHeading (float pitch, float heading)
        {
            targetPHR.x = pitch;
            targetPHR.y = heading;
        }

        /// <summary>
        /// The target roll, in degrees. <c>NaN</c> if no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float TargetRoll {
            get { return (float)targetPHR.z; }
            set { targetPHR.z = value; }
        }

        /// <summary>
        /// The state of SAS.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SAS"/></remarks>
        [KRPCProperty]
        public bool SAS {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        /// <summary>
        /// The current <see cref="SASMode"/>.
        /// These modes are equivalent to the mode buttons to
        /// the left of the navball that appear when SAS is enabled.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SASMode"/></remarks>
        [KRPCProperty]
        public SASMode SASMode {
            get { return Control.GetSASMode (InternalVessel); }
            set { Control.SetSASMode (InternalVessel, value); }
        }

        /// <summary>
        /// Maximum target rotation speed to pass to the rotation rate controller, in radians per second. Defaults to 1.
        /// </summary>
        [KRPCProperty]
        public float MaxRotationSpeed { get; set; }

        /// <summary>
        /// Whether the rotation rate controllers PID parameters should be automatically tuned using the vessels moment of inertia and available torque. Defaults to <c>true</c>.
        /// See <see cref="TimeToPeak"/> and  <see cref="Overshoot"/>.
        /// </summary>
        [KRPCProperty]
        public bool AutoTune {
            get { return rotationRateController.AutoTune; }
            set { rotationRateController.AutoTune = value; }
        }

        /// <summary>
        /// The target time to peak used to autotune the rotation rate controller, in seconds. Defaults to 1 second.
        /// </summary>
        [KRPCProperty]
        public float TimeToPeak {
            get { return rotationRateController.TimeToPeak; }
            set { rotationRateController.TimeToPeak = value; }
        }

        /// <summary>
        /// The target overshoot percentage used to autotune the rotation rate controller, as a value between 0 and 1. Defaults to 0.01.
        /// </summary>
        [KRPCProperty]
        public float Overshoot {
            get { return rotationRateController.Overshoot; }
            set { rotationRateController.Overshoot = value; }
        }

        /// <summary>
        /// PID gains for the pitch rotation rate controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically and will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 PitchPIDGains {
            get {
                var pid = rotationRateController.PitchPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set { rotationRateController.PitchPID.SetParameters (value.Item1, value.Item2, value.Item3); }
        }

        /// <summary>
        /// PID gains for the roll rotation rate controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically and will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 RollPIDGains {
            get {
                var pid = rotationRateController.RollPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set { rotationRateController.RollPID.SetParameters (value.Item1, value.Item2, value.Item3); }
        }

        /// <summary>
        /// PID gains for the yaw rotation rate controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically and will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 YawPIDGains {
            get {
                var pid = rotationRateController.YawPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set { rotationRateController.YawPID.SetParameters (value.Item1, value.Item2, value.Item3); }
        }

        /// <summary>
        /// The direction vector that the SAS autopilot is trying to hold in world space
        /// </summary>
        Vector3d SASTargetDirection ()
        {
            // Stability assist
            if (SASMode == SASMode.StabilityAssist)
                throw new InvalidOperationException ("No target direction in stability assist mode");

            // Maneuver node
            if (SASMode == SASMode.Maneuver) {
                var node = InternalVessel.patchedConicSolver.maneuverNodes.OrderBy (x => x.UT).FirstOrDefault ();
                if (node == null)
                    throw new InvalidOperationException ("No maneuver node");
                return new Node (InternalVessel, node).WorldBurnVector;
            }

            // Orbital directions, in different speed modes
            if (SASMode == SASMode.Prograde || SASMode == SASMode.Retrograde ||
                SASMode == SASMode.Normal || SASMode == SASMode.AntiNormal ||
                SASMode == SASMode.Radial || SASMode == SASMode.AntiRadial) {

                if (Control.GetSpeedMode () == SpeedMode.Orbit) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.forward);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.back);
                    case SASMode.Radial:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.left);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Orbital (InternalVessel).DirectionToWorldSpace (Vector3d.right);
                    }
                } else if (Control.GetSpeedMode () == SpeedMode.Surface) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.SurfaceVelocity (InternalVessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.SurfaceVelocity (InternalVessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Object (InternalVessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (InternalVessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (InternalVessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (InternalVessel).DirectionToWorldSpace (Vector3d.left);
                    }
                } else if (Control.GetSpeedMode () == SpeedMode.Target) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return InternalVessel.GetWorldVelocity () - FlightGlobals.fetch.VesselTarget.GetWorldVelocity ();
                    case SASMode.Retrograde:
                        return FlightGlobals.fetch.VesselTarget.GetWorldVelocity () - InternalVessel.GetWorldVelocity ();
                    case SASMode.Normal:
                        return ReferenceFrame.Object (InternalVessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (InternalVessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (InternalVessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (InternalVessel).DirectionToWorldSpace (Vector3d.left);
                    }
                }
                throw new InvalidOperationException ("Unknown speed mode for orbital direction");
            }

            // Target and anti-target
            if (SASMode == SASMode.Target || SASMode == SASMode.AntiTarget) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target == null)
                    throw new InvalidOperationException ("No target");
                var direction = target.GetWorldPosition () - InternalVessel.GetWorldPos3D ();
                if (SASMode == SASMode.AntiTarget)
                    direction *= -1;
                return direction;
            }

            throw new InvalidOperationException ("Unknown SAS mode");
        }

        internal static bool Fly (global::Vessel vessel, PilotAddon.ControlInputs state)
        {
            // Get the auto-pilot object. Do nothing if there is no auto-pilot engaged for this vessel.
            if (!engaged.ContainsKey (vessel.id))
                return false;
            var autoPilot = engaged [vessel.id];
            if (autoPilot == null)
                return false;
            // If the client that engaged the auto-pilot has disconnected, disengage and reset the auto-pilot
            if (autoPilot.requestingClient != null && !autoPilot.requestingClient.Connected) {
                autoPilot.ReferenceFrame = ReferenceFrame.Surface (vessel);
                autoPilot.targetPHR = Vector3.zero;
                autoPilot.targetPHR.z = double.NaN;
                autoPilot.Disengage ();
                return false;
            }
            // Run the auto-pilot
            autoPilot.DoAutoPiloting (state);
            return true;
        }

        void DoAutoPiloting (PilotAddon.ControlInputs state)
        {
            SAS = false;
            rotationRateController.Target = ComputeTargetRotationRate ();
            rotationRateController.Update (state);
        }

        Vector3 ComputeTargetRotationRate ()
        {
            var currentRotation = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation);
            var currentDirection = ReferenceFrame.DirectionFromWorldSpace (InternalVessel.ReferenceTransform.up);

            var targetDirection = TargetDirectionVector;

            QuaternionD rotation;
            if (Vector3.Angle (currentDirection, targetDirection) < 5 && !double.IsNaN (targetPHR.z)) {
                // Pointing close to target direction and roll angle set.
                // Compute rotation necessary to rotate from currentRotation -> targetRotation
                var targetRotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (targetPHR);
                rotation = targetRotation * currentRotation.Inverse ();
            } else {
                // Pointing far away from target direction or roll angle not set. Exclude roll.
                // Compute rotation necessary to rotate from currentDirection -> targetDirection
                //FIXME: QuaternionD.FromToRotation method not available at runtime
                rotation = Quaternion.FromToRotation (currentDirection, targetDirection);
            }

            // Compute angles for the rotation in pitch (x), roll (y), yaw (z) axes of the reference frame
            float angleFloat;
            Vector3 axisFloat;
            //FIXME: QuaternionD.ToAngleAxis method not available at runtime
            ((Quaternion)rotation).ToAngleAxis (out angleFloat, out axisFloat);
            double angle = GeometryExtensions.ClampAngle180 (angleFloat);
            Vector3d axis = axisFloat;
            var angles = axis * angle;
            var rate = (MaxRotationSpeed / 180) * angles;
            return -rate;
        }
    }
}
