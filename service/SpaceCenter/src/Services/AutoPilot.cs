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
        Vector3d targetDirection;

        internal AutoPilot (global::Vessel vessel)
        {
            if (!engaged.ContainsKey (vessel.id))
                engaged [vessel.id] = null;
            vesselId = vessel.id;
            rotationRateController = new RotationRateController (vessel);
            ReferenceFrame = ReferenceFrame.Surface (vessel);
            TargetDirection = null;
            TargetRoll = float.NaN;
            RotationSpeedMultiplier = 1f;
            RollSpeedMultiplier = 1f;
            MaxRollSpeed = 1f;
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
            requestingClient = KRPC.KRPCCore.Context.RPCClient;
            engaged [vesselId] = this;
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
        /// Blocks until the vessel is pointing in the target direction (if set) and has the target roll (if set).
        /// </summary>
        [KRPCMethod]
        public void Wait ()
        {
            if (Error > 0.5f || RollError > 0.5f || InternalVessel.angularVelocity.magnitude > 0.05f)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        /// <summary>
        /// The error, in degrees, between the direction the ship has been asked
        /// to point in and the direction it is pointing in. Returns zero if the auto-pilot
        /// has not been engaged, SAS is not enabled, SAS is in stability assist mode,
        /// or no target direction is set.
        /// </summary>
        [KRPCProperty]
        public float Error {
            get {
                if (engaged [vesselId] == this && targetDirection != Vector3d.zero)
                    return Vector3.Angle (InternalVessel.ReferenceTransform.up, ReferenceFrame.DirectionToWorldSpace (targetDirection));
                else if (engaged [vesselId] != this && SAS && SASMode != SASMode.StabilityAssist)
                    return Vector3.Angle (InternalVessel.ReferenceTransform.up, SASTargetDirection ());
                else
                    return 0f;
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
                if (engaged [vesselId] != this || Double.IsNaN (TargetRoll))
                    return 0f;
                var currentRoll = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                return (float)Math.Abs (TargetRoll - currentRoll);
            }
        }

        /// <summary>
        /// The reference frame for the target direction (<see cref="AutoPilot.TargetDirection"/>).
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// The target direction. <c>null</c> if no target direction is set.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TargetDirection {
            get { return targetDirection == Vector3d.zero ? null : targetDirection.ToTuple (); }
            set { targetDirection = value == null ? Vector3d.zero : value.ToVector ().normalized; }
        }

        /// <summary>
        /// Set (<see cref="AutoPilot.TargetDirection"/>) from a pitch and heading angle.
        /// </summary>
        /// <param name="pitch">Target pitch angle, in degrees between -90° and +90°.</param>
        /// <param name="heading">Target heading angle, in degrees between 0° and 360°.</param>
        [KRPCMethod]
        public void TargetPitchAndHeading (float pitch, float heading)
        {
            var rotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (new Vector3 (pitch, heading, 0));
            targetDirection = rotation * Vector3.up;
        }

        /// <summary>
        /// The target roll, in degrees. <c>NaN</c> if no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float TargetRoll { get; set; }

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
        /// Target rotation speed multiplier. Defaults to 1.
        /// </summary>
        [KRPCProperty]
        public float RotationSpeedMultiplier { get; set; }

        /// <summary>
        /// Maximum target rotation speed. Defaults to 1.
        /// </summary>
        [KRPCProperty]
        public float MaxRotationSpeed { get; set; }

        /// <summary>
        /// Target roll speed multiplier. Defaults to 1.
        /// </summary>
        [KRPCProperty]
        public float RollSpeedMultiplier { get; set; }

        /// <summary>
        /// Maximum target roll speed. Defaults to 1.
        /// </summary>
        [KRPCProperty]
        public float MaxRollSpeed { get; set; }

        /// <summary>
        /// Sets the gains for the rotation rate PID controller.
        /// </summary>
        /// <param name="kp">Proportional gain.</param>
        /// <param name="ki">Integral gain.</param>
        /// <param name="kd">Derivative gain.</param>
        [KRPCMethod]
        public void SetPIDParameters (float kp = 1, float ki = 0, float kd = 0)
        {
            rotationRateController.PID.SetParameters (kp, ki, kd);
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
                autoPilot.TargetDirection = null;
                autoPilot.TargetRoll = float.NaN;
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
            var currentDirection = ReferenceFrame.DirectionFromWorldSpace (InternalVessel.ReferenceTransform.up);
            rotationRateController.ReferenceFrame = ReferenceFrame;
            var targetRotation = Vector3d.zero;
            if (targetDirection != Vector3d.zero)
                targetRotation += (Vector3.Cross (targetDirection, currentDirection) * RotationSpeedMultiplier).ClampMagnitude (0f, MaxRotationSpeed);
            if (!Double.IsNaN (TargetRoll)) {
                float currentRoll = (float)ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                var rollError = GeometryExtensions.NormAngle (TargetRoll - currentRoll) * (Math.PI / 180f);
                targetRotation += targetDirection * (rollError * RollSpeedMultiplier).Clamp (-MaxRollSpeed, MaxRollSpeed);
            }
            rotationRateController.Target = targetRotation;
            rotationRateController.Update (state);
        }
    }
}
