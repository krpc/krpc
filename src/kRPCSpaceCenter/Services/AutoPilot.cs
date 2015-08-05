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
    /// The behavior of the SAS auto-pilot. See <see cref="AutoPilot.SASMode"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SASMode
    {
        /// <summary>
        /// Stability assist mode. Dampen out any rotation.
        /// </summary>
        StabilityAssist,
        /// <summary>
        /// Point in the burn direction of the next maneuver node.
        /// </summary>
        Maneuver,
        /// <summary>
        /// Point in the prograde direction.
        /// </summary>
        Prograde,
        /// <summary>
        /// Point in the retrograde direction.
        /// </summary>
        Retrograde,
        /// <summary>
        /// Point in the orbit normal direction.
        /// </summary>
        Normal,
        /// <summary>
        /// Point in the orbit anti-normal direction.
        /// </summary>
        AntiNormal,
        /// <summary>
        /// Point in the orbit radial direction.
        /// </summary>
        Radial,
        /// <summary>
        /// Point in the orbit anti-radial direction.
        /// </summary>
        AntiRadial,
        /// <summary>
        /// Point in the direction of the current target.
        /// </summary>
        Target,
        /// <summary>
        /// Point away from the current target.
        /// </summary>
        AntiTarget
    }

    /// <summary>
    /// See <see cref="AutoPilot.SpeedMode"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SpeedMode
    {
        /// <summary>
        /// Speed is relative to the vessel's orbit.
        /// </summary>
        Orbit,
        /// <summary>
        /// Speed is relative to the surface of the body being orbited.
        /// </summary>
        Surface,
        /// <summary>
        /// Speed is relative to the current target.
        /// </summary>
        Target
    }

    /// <summary>
    /// Provides basic auto-piloting utilities for a vessel.
    /// Created by calling <see cref="Vessel.AutoPilot"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class AutoPilot : Equatable<AutoPilot>
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

        readonly global::Vessel vessel;
        readonly RotationRateController rotationRateController;
        static IDictionary<global::Vessel, AutoPilot> engaged = new Dictionary<global::Vessel, AutoPilot> ();
        IClient requestingClient;
        ReferenceFrame referenceFrame;
        Vector3 targetDirection;
        float targetRoll;

        internal AutoPilot (global::Vessel vessel)
        {
            this.vessel = vessel;
            rotationRateController = new RotationRateController (vessel);
            if (!engaged.ContainsKey (vessel))
                engaged [vessel] = null;
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
            get { return GetSASMode (vessel); }
            set { SetSASMode (vessel, value); }
        }

        internal static SASMode GetSASMode (global::Vessel vessel)
        {
            return vessel.Autopilot.Mode.ToSASMode ();
        }

        internal static void SetSASMode (global::Vessel vessel, SASMode value)
        {
            var mode = value.FromSASMode ();
            if (!vessel.Autopilot.CanSetMode (mode))
                throw new InvalidOperationException ("Cannot set SAS mode of vessel");
            vessel.Autopilot.SetMode (mode);
            // Update the UI buttons
            var modeIndex = (int)vessel.Autopilot.Mode;
            var modeButtons = UnityEngine.Object.FindObjectOfType<VesselAutopilotUI> ().modeButtons;
            modeButtons.ElementAt<RUIToggleButton> (modeIndex).SetTrue (true, true);
        }

        /// <summary>
        /// The current <see cref="SpeedMode"/> of the navball.
        /// This is the mode displayed next to the speed at the top of the navball.
        /// </summary>
        [KRPCProperty]
        public SpeedMode SpeedMode {
            get { return FlightUIController.speedDisplayMode.ToSpeedMode (); }
            set {
                var startMode = FlightUIController.speedDisplayMode;
                var mode = value.FromSpeedMode ();
                while (FlightUIController.speedDisplayMode != mode) {
                    FlightUIController.fetch.cycleSpdModes ();
                    if (FlightUIController.speedDisplayMode == startMode)
                        break;
                }
            }
        }

        /// <summary>
        /// Points the vessel in the specified direction, and holds it there. Setting
        /// the roll angle is optional.
        ///
        /// If wait is <c>false</c> (the default) this method returns immediately, and
        /// the auto-pilot continues to rotate the vessel. If wait is <c>true</c>, this
        /// method returns when the auto-pilot has rotated the vessel into the
        /// requested orientation.
        ///
        /// The auto-pilot is disengaged either when <see cref="AutoPilot.Disengage"/> is
        /// called, or when the client that requested the auto-pilot command disconnects.
        /// </summary>
        /// <param name="pitch">The desired pitch above/below the horizon, in degrees.
        /// A value between -90° and +90° degrees.</param>
        /// <param name="heading">The desired heading in degrees. A value between 0° and 360°.</param>
        /// <param name="roll">Optional desired roll angle relative to the horizon, in degrees.
        /// A value between -180° and +180°.</param>
        /// <param name="referenceFrame">The reference frame that the pitch, heading and roll are in.
        /// Defaults to the vessels surface reference frame.</param>
        /// <param name="wait">If <c>true</c>, this method returns when the auto-pilot has rotated the
        /// vessel into the requested orientation.</param>
        [KRPCMethod]
        public void SetRotation (float pitch, float heading, float roll = float.NaN, ReferenceFrame referenceFrame = null, bool wait = false)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Surface (vessel);
            this.referenceFrame = referenceFrame;
            var rotation = GeometryExtensions.QuaternionFromPitchHeadingRoll (new Vector3 (pitch, heading, 0));
            targetDirection = rotation * Vector3.up;
            targetRoll = roll;
            Engage ();
            if (wait)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        /// <summary>
        /// Points the vessel along the specified direction vector, and holds it
        /// there. Setting the roll angle is optional.
        ///
        /// If wait is <c>false</c> (the default) this method returns immediately, and
        /// the auto-pilot continues to rotate the vessel. If wait is <c>true</c>, this
        /// method returns when the auto-pilot has rotated the vessel into the
        /// requested orientation.
        ///
        /// The auto-pilot is disengaged either when <see cref="AutoPilot.Disengage"/>
        /// is called, or when the client that requested the auto-pilot command
        /// disconnects.
        /// </summary>
        /// <param name="direction">The desired direction (pitch and heading) as a unit vector.</param>
        /// <param name="roll">Optional desired roll angle relative to the horizon, in degrees.
        /// A value between -180° and 180°.</param>
        /// <param name="referenceFrame">The reference frame that the direction vector is in. Defaults
        /// to the vessels surface reference frame.</param>
        /// <param name="wait">If true, this method returns when the auto-pilot has rotated the vessel
        /// into the requested orientation.</param>
        [KRPCMethod]
        public void SetDirection (Tuple3 direction, float roll = float.NaN, ReferenceFrame referenceFrame = null, bool wait = false)
        {
            if (referenceFrame == null)
                referenceFrame = ReferenceFrame.Surface (vessel);
            this.referenceFrame = referenceFrame;
            targetDirection = direction.ToVector ();
            targetRoll = roll;
            Engage ();
            if (wait)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        void Wait ()
        {
            if (Error > 0.5f || RollError > 0.5f || vessel.angularVelocity.magnitude > 0.05f)
                throw new YieldException (new ParameterizedContinuationVoid (Wait));
        }

        void Engage ()
        {
            requestingClient = KRPC.KRPCServer.Context.RPCClient;
            engaged [vessel] = this;
        }

        /// <summary>
        /// Disengage the auto-pilot. Has no effect unless <see cref="AutoPilot.SetRotation"/>
        /// or <see cref="AutoPilot.SetDirection"/> have been called previously.
        /// </summary>
        /// <remarks>
        /// This will disable <see cref="Control.SAS"/>.
        /// </remarks>
        [KRPCMethod]
        public void Disengage ()
        {
            requestingClient = null;
            engaged [vessel] = null;
        }

        /// <summary>
        /// The error, in degrees, between the direction the ship has been asked
        /// to point in and the actual direction it is pointing in. If the auto-pilot
        /// has not been engaged, returns zero.
        /// </summary>
        [KRPCProperty]
        public float Error {
            get {
                if (engaged [vessel] == this)
                    return Vector3.Angle (vessel.ReferenceTransform.up, referenceFrame.DirectionToWorldSpace (targetDirection));
                else if (SAS && SASMode != SASMode.StabilityAssist)
                    return Vector3.Angle (vessel.ReferenceTransform.up, SASTargetDirection ());
                else
                    return 0f;
            }
        }

        /// <summary>
        /// The error, in degrees, between the roll the ship has been asked to be
        /// in and the actual roll. If the auto-pilot has not been engaged, returns zero.
        /// </summary>
        [KRPCProperty]
        public float RollError {
            get {
                if (engaged [vessel] != this || Double.IsNaN (targetRoll))
                    return 0f;
                var currentRoll = referenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                return (float)Math.Abs (targetRoll - currentRoll);
            }
        }

        /// <summary>
        /// Set the gain coefficients for the autopilot attitude PID controller.
        /// </summary>
        /// <param name="Kp">Proportional gain.</param>
        /// <param name="Ki">Integral gain.</param>
        /// <param name="Kd">Derivative gain.</param>
        [KRPCMethod]
        public void SetPIDParameters (float Kp, float Ki, float Kd)
        {
            rotationRateController.pid.SetParameters (Kp, Ki, Kd, 1f);
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

                if (SpeedMode == SpeedMode.Orbit) {
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
                } else if (SpeedMode == SpeedMode.Surface) {
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
                } else if (SpeedMode == SpeedMode.Target) {
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
            var currentDirection = referenceFrame.DirectionFromWorldSpace (vessel.ReferenceTransform.up);
            rotationRateController.ReferenceFrame = referenceFrame;
            rotationRateController.Target = Vector3.Cross (targetDirection, currentDirection);
            if (!Double.IsNaN (targetRoll)) {
                float currentRoll = (float)referenceFrame.RotationFromWorldSpace (vessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                rotationRateController.Target += targetDirection * ((targetRoll - currentRoll) / 90f);
            }
            rotationRateController.Update (state);
            pitch = state.Pitch;
            yaw = state.Yaw;
            roll = state.Roll;
        }

        float pitch, yaw, roll;

        [KRPCProperty]
        public float Pitch {
            get { return pitch; }
        }

        [KRPCProperty]
        public float Yaw {
            get { return yaw; }
        }

        [KRPCProperty]
        public float Roll {
            get { return roll; }
        }
    }
}
