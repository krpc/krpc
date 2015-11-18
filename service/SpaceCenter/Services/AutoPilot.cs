using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using KRPCSpaceCenter.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPCSpaceCenter.Services
{
    /// <summary>
    /// Provides basic auto-piloting utilities for a vessel.
    /// Created by calling <see cref="Vessel.AutoPilot"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class AutoPilot : Equatable<AutoPilot>
    {
        static IDictionary<global::Vessel, AutoPilot> engaged = new Dictionary<global::Vessel, AutoPilot> ();
        readonly global::Vessel vessel;
        readonly RotationRateController rotationRateController;
        IClient requestingClient;
        Vector3d targetDirection;

        internal AutoPilot (global::Vessel vessel)
        {
            if (!engaged.ContainsKey (vessel))
                engaged [vessel] = null;
            this.vessel = vessel;
            rotationRateController = new RotationRateController (vessel);
            ReferenceFrame = ReferenceFrame.Surface (vessel);
            TargetDirection = null;
            TargetRoll = float.NaN;
            RotationSpeedMultiplier = 1f;
            RollSpeedMultiplier = 1f;
            MaxRollSpeed = 1f;
            MaxRotationSpeed = 1f;
        }

        public override bool Equals (AutoPilot obj)
        {
            return vessel == obj.vessel;
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode ();
        }

        /// <summary>
        /// Engage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Engage ()
        {
            requestingClient = KRPC.KRPCServer.Context.RPCClient;
            engaged [vessel] = this;
        }

        /// <summary>
        /// Disengage the auto-pilot.
        /// </summary>
        [KRPCMethod]
        public void Disengage ()
        {
            requestingClient = null;
            engaged [vessel] = null;
        }

        /// <summary>
        /// Blocks until the vessel is pointing in the target direction (if set) and has the target roll (if set).
        /// </summary>
        [KRPCMethod]
        public void Wait ()
        {
            if (Error > 0.5f || RollError > 0.5f || vessel.angularVelocity.magnitude > 0.05f)
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
                if (engaged [vessel] == this && targetDirection != Vector3d.zero)
                    return Vector3.Angle (vessel.ReferenceTransform.up, ReferenceFrame.DirectionToWorldSpace (targetDirection));
                else if (engaged [vessel] != this && SAS && SASMode != SASMode.StabilityAssist)
                    return Vector3.Angle (vessel.ReferenceTransform.up, SASTargetDirection ());
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
                if (engaged [vessel] != this || Double.IsNaN (TargetRoll))
                    return 0f;
                var currentRoll = ReferenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
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
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        /// <summary>
        /// The current <see cref="SASMode"/>.
        /// These modes are equivalent to the mode buttons to
        /// the left of the navball that appear when SAS is enabled.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SASMode"/></remarks>
        [KRPCProperty]
        public SASMode SASMode {
            get { return Control.GetSASMode (vessel); }
            set { Control.SetSASMode (vessel, value); }
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
        /// <param name="Kp">Proportional gain.</param>
        /// <param name="Ki">Integral gain.</param>
        /// <param name="Kd">Derivative gain.</param>
        [KRPCMethod]
        public void SetPIDParameters (float Kp = 1, float Ki = 0, float Kd = 0)
        {
            rotationRateController.pid.SetParameters (Kp, Ki, Kd);
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
                var node = vessel.patchedConicSolver.maneuverNodes.OrderBy (x => x.UT).FirstOrDefault ();
                if (node == null)
                    throw new InvalidOperationException ("No maneuver node");
                return new Node (node).WorldBurnVector;
            }

            // Orbital directions, in different speed modes
            if (SASMode == SASMode.Prograde || SASMode == SASMode.Retrograde ||
                SASMode == SASMode.Normal || SASMode == SASMode.AntiNormal ||
                SASMode == SASMode.Radial || SASMode == SASMode.AntiRadial) {

                if (Control.GetSpeedMode () == SpeedMode.Orbit) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.forward);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.back);
                    case SASMode.Radial:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.left);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Orbital (vessel).DirectionToWorldSpace (Vector3d.right);
                    }
                } else if (Control.GetSpeedMode () == SpeedMode.Surface) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return ReferenceFrame.SurfaceVelocity (vessel).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.Retrograde:
                        return ReferenceFrame.SurfaceVelocity (vessel).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Normal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.left);
                    }
                } else if (Control.GetSpeedMode () == SpeedMode.Target) {
                    switch (SASMode) {
                    case SASMode.Prograde:
                        return vessel.GetWorldVelocity () - FlightGlobals.fetch.VesselTarget.GetWorldVelocity ();
                    case SASMode.Retrograde:
                        return FlightGlobals.fetch.VesselTarget.GetWorldVelocity () - vessel.GetWorldVelocity ();
                    case SASMode.Normal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.up);
                    case SASMode.AntiNormal:
                        return ReferenceFrame.Object (vessel.orbit.referenceBody).DirectionToWorldSpace (Vector3d.down);
                    case SASMode.Radial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.right);
                    case SASMode.AntiRadial:
                        return ReferenceFrame.Surface (vessel).DirectionToWorldSpace (Vector3d.left);
                    }
                }
                throw new InvalidOperationException ("Unknown speed mode for orbital direction");
            }

            // Target and anti-target
            if (SASMode == SASMode.Target || SASMode == SASMode.AntiTarget) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target == null)
                    throw new InvalidOperationException ("No target");
                var direction = target.GetWorldPosition () - vessel.GetWorldPos3D ();
                if (SASMode == SASMode.AntiTarget)
                    direction *= -1;
                return direction;
            }

            throw new InvalidOperationException ("Unknown SAS mode");
        }

        internal static bool Fly (global::Vessel vessel, PilotAddon.ControlInputs state)
        {
            // Get the auto-pilot object. Do nothing if there is no auto-pilot engaged for this vessel.
            if (!engaged.ContainsKey (vessel))
                return false;
            var autoPilot = engaged [vessel];
            if (autoPilot == null)
                return false;
            // If the client that engaged the auto-pilot has disconnected, disengage the auto-pilot
            if (autoPilot.requestingClient != null && !autoPilot.requestingClient.Connected) {
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
            var currentDirection = ReferenceFrame.DirectionFromWorldSpace (vessel.ReferenceTransform.up);
            rotationRateController.ReferenceFrame = ReferenceFrame;
            var targetRotation = Vector3d.zero;
            if (targetDirection != Vector3d.zero)
                targetRotation += (Vector3.Cross (targetDirection, currentDirection) * RotationSpeedMultiplier).ClampMagnitude (0f, MaxRotationSpeed);
            if (!Double.IsNaN (TargetRoll)) {
                float currentRoll = (float)ReferenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                var rollError = GeometryExtensions.NormAngle (TargetRoll - currentRoll) * (Math.PI / 180f);
                targetRotation += targetDirection * (rollError * RollSpeedMultiplier).Clamp (-MaxRollSpeed, MaxRollSpeed);
            }
            rotationRateController.Target = targetRotation;
            rotationRateController.Update (state);
        }
    }
}
