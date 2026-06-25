using System;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.AutoPilot;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Provides basic auto-piloting utilities for a vessel.
    /// Created by calling <see cref="Vessel.AutoPilot"/>.
    /// </summary>
    /// <remarks>
    /// If a client engages the auto-pilot and then closes its connection to the server,
    /// the auto-pilot will be disengaged and its target reference frame, direction and roll
    /// reset to default.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class AutoPilot : Equatable<AutoPilot>
    {
        static readonly IDictionary<Guid, AutoPilot> engaged = new Dictionary<Guid, AutoPilot> ();
        readonly Guid vesselId;
        readonly AttitudeController attitudeController;
        IClient requestingClient;

        internal AutoPilot (global::Vessel vessel)
        {
            if (!engaged.ContainsKey (vessel.id))
                engaged [vessel.id] = null;
            vesselId = vessel.id;
            attitudeController = new AttitudeController (vessel);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (AutoPilot other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId;
        }

        /// <summary>
        /// Hash code for the object.
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
        /// Whether the auto-pilot is engaged.
        /// Setting to <c>true</c> engages the auto-pilot; setting to <c>false</c> disengages it.
        /// </summary>
        [KRPCProperty]
        public bool Engaged {
            get { return engaged [vesselId] == this; }
            set {
                if (value) {
                    requestingClient = CallContext.Client;
                    engaged [vesselId] = this;
                    attitudeController.Start ();
                } else {
                    requestingClient = null;
                    engaged [vesselId] = null;
                }
            }
        }

        /// <summary>
        /// Blocks until the vessel is pointing in the target direction and has
        /// the target roll (if set). Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        [KRPCMethod]
        public void Wait ()
        {
            if (Error > 1f || InternalVessel.GetComponent<Rigidbody> ().angularVelocity.magnitude > 0.05f)
                throw new YieldException<Action> (Wait);
        }

        /// <summary>
        /// The error, in degrees, between the direction the ship has been asked
        /// to point in and the direction it is pointing in. Throws an exception if the auto-pilot
        /// has not been engaged and SAS is not enabled or is in stability assist mode.
        /// </summary>
        [KRPCProperty]
        public float Error {
            get {
                if (Engaged) {
                    if (!double.IsNaN (attitudeController.TargetRoll)) {
                        var currentRotation = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation);
                        var targetRotation = attitudeController.TargetRotation;
                        var rotation = targetRotation * currentRotation.Inverse ();
                        double angle;
                        Vector3d axis;
                        GeometryExtensions.ToAngleAxis (rotation, out angle, out axis);
                        return Math.Abs (GeometryExtensions.NormAngle ((float)angle));
                    } else {
                        return GeometryExtensions.NormAngle (Vector3.Angle (InternalVessel.ReferenceTransform.up, ReferenceFrame.DirectionToWorldSpace (attitudeController.TargetDirection)));
                    }
                } else if (!Engaged && SAS && SASMode != SASMode.StabilityAssist) {
                    return GeometryExtensions.NormAngle (Vector3.Angle (InternalVessel.ReferenceTransform.up, SASTargetDirection ()));
                } else {
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                }
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target pitch.
        /// Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public float PitchError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                var currentPitch = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().x;
                return (float)Math.Abs (GeometryExtensions.ClampAngle180 (attitudeController.TargetPitch - currentPitch));
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target heading.
        /// Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public float HeadingError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                var currentHeading = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().y;
                return (float)Math.Abs (GeometryExtensions.ClampAngle180 (attitudeController.TargetHeading - currentHeading));
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target roll.
        /// Throws an exception if the auto-pilot has not been engaged or no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float RollError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                if (double.IsNaN (attitudeController.TargetRoll))
                    throw new InvalidOperationException ("No target roll has been set");
                var currentRoll = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ().z;
                return (float)Math.Abs (GeometryExtensions.ClampAngle180 (attitudeController.TargetRoll - currentRoll));
            }
        }

        /// <summary>
        /// The reference frame for the target direction (<see cref="TargetDirection"/>).
        /// </summary>
        /// <remarks>
        /// An error will be thrown if this property is set to a reference frame that rotates with
        /// the vessel being controlled, as it is impossible to rotate the vessel in such a
        /// reference frame.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return attitudeController.ReferenceFrame; }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("ReferenceFrame");
                var rotatesWithVessel = false;
                switch (value.Type) {
                case ReferenceFrameType.Vessel:
                    rotatesWithVessel = value.Vessel.Id == vesselId;
                    break;
                case ReferenceFrameType.Part:
                case ReferenceFrameType.PartCenterOfMass:
                case ReferenceFrameType.Thrust:
                    rotatesWithVessel = value.Part.InternalPart.vessel.id == vesselId;
                    break;
                case ReferenceFrameType.DockingPort:
                    rotatesWithVessel = value.DockingPort.Part.InternalPart.vessel.id == vesselId;
                    break;
                }
                if (rotatesWithVessel) {
                    throw new ArgumentException ("Invalid reference frame; must not rotate with the vessel");
                }
                attitudeController.ReferenceFrame = value;
            }
        }

        /// <summary>
        /// The target pitch, in degrees, between -90° and +90°.
        /// </summary>
        [KRPCProperty]
        public float TargetPitch {
            get { return (float)attitudeController.TargetPitch; }
            set { attitudeController.TargetPitch = value; }
        }

        /// <summary>
        /// The target heading, in degrees, between 0° and 360°.
        /// </summary>
        [KRPCProperty]
        public float TargetHeading {
            get { return (float)attitudeController.TargetHeading; }
            set { attitudeController.TargetHeading = value; }
        }

        /// <summary>
        /// The target roll, in degrees. <c>NaN</c> if no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float TargetRoll {
            get { return (float)attitudeController.TargetRoll; }
            set { attitudeController.TargetRoll = value; }
        }

        /// <summary>
        /// Set target pitch and heading angles.
        /// </summary>
        /// <param name="pitch">Target pitch angle, in degrees between -90° and +90°.</param>
        /// <param name="heading">Target heading angle, in degrees between 0° and 360°.</param>
        [KRPCMethod]
        public void TargetPitchAndHeading (float pitch, float heading)
        {
            attitudeController.TargetPitch = pitch;
            attitudeController.TargetHeading = heading;
        }

        /// <summary>
        /// Disengages the auto-pilot and resets all configuration parameters to their defaults.
        /// Also resets the target pitch, heading and roll.
        /// </summary>
        [KRPCMethod]
        public void Reset ()
        {
            Engaged = false;
            attitudeController.Reset ();
        }

        /// <summary>
        /// Direction vector corresponding to the target pitch and heading.
        /// This is in the reference frame specified by <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TargetDirection {
            get { return attitudeController.TargetDirection.ToTuple (); }
            set { attitudeController.SetTargetDirection (value.ToVector ()); }
        }

        /// <summary>
        /// The target rotation quaternion. Setting this also sets the target roll.
        /// This is in the reference frame specified by <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public Tuple4 TargetRotation {
            get { return attitudeController.TargetRotation.ToTuple (); }
            set { attitudeController.SetTargetRotation (value.ToQuaternion ()); }
        }

        /// <summary>
        /// The state of SAS.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SAS"/></remarks>
        [KRPCProperty]
        public bool SAS {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set {
                if (value && Engaged)
                    throw new InvalidOperationException ("SAS cannot be enabled when the auto-pilot is engaged");
                InternalVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value);
            }
        }

        /// <summary>
        /// The current <see cref="SASMode"/>.
        /// These modes are equivalent to the mode buttons to the left of the navball that appear
        /// when SAS is enabled.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SASMode"/></remarks>
        [KRPCProperty]
        public SASMode SASMode {
            get { return Control.GetSASMode (InternalVessel); }
            set { Control.SetSASMode (InternalVessel, value); }
        }

        /// <summary>
        /// The direction error, in degrees, above which roll blending is fully suppressed.
        /// Defaults to 20 degrees.
        /// </summary>
        [KRPCProperty]
        public double RollStartAngle {
            get { return attitudeController.RollStartAngle; }
            set { attitudeController.RollStartAngle = value; }
        }

        /// <summary>
        /// The direction error, in degrees, below which roll is fully engaged.
        /// Roll blends linearly between <see cref="RollStartAngle"/> and this value.
        /// Defaults to 15 degrees.
        /// </summary>
        [KRPCProperty]
        public double RollEngageAngle {
            get { return attitudeController.RollEngageAngle; }
            set { attitudeController.RollEngageAngle = value; }
        }

        /// <summary>
        /// The maximum angular velocity of the vessel, in rad/s, for each of the pitch, roll
        /// and yaw axes. Limits the target angular velocity computed by the bang-bang profile so
        /// that vessels with very high torque availability do not spin faster than desired.
        /// Defaults to 1 rad/s for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 MaxAngularVelocity {
            get { return attitudeController.MaxAngularVelocity.ToTuple (); }
            set { attitudeController.MaxAngularVelocity = value.ToVector (); }
        }

        /// <summary>
        /// The angle at which the autopilot considers the vessel to be pointing
        /// close to the target.
        /// This determines the midpoint of the target velocity attenuation function.
        /// A vector of three angles, in degrees, one for each of the pitch, roll and yaw axes.
        /// Defaults to 1° for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 AttenuationAngle {
            get { return attitudeController.AttenuationAngle.ToTuple (); }
            set { attitudeController.AttenuationAngle = value.ToVector (); }
        }

        /// <summary>
        /// Whether the rotation rate controllers PID parameters should be automatically tuned
        /// using the vessels moment of inertia and available torque. Defaults to <c>true</c>.
        /// See <see cref="TimeToPeak"/> and <see cref="Overshoot"/>.
        /// </summary>
        [KRPCProperty]
        public bool AutoTune {
            get { return attitudeController.AutoTune; }
            set { attitudeController.AutoTune = value; }
        }

        /// <summary>
        /// Whether to apply a linear PID-lag correction to the stopping-distance feedforward.
        /// Defaults to <c>true</c>.
        /// When <c>true</c>, the outer-loop velocity profile uses <c>max(omega^2/(2*alpha), omega/bandwidth)</c>
        /// as the predicted stopping distance. The linear term (<c>omega/bandwidth</c>) is larger
        /// when the PID is unsaturated and decelerating more slowly than full torque; including it
        /// prevents overshoot on small rigid vessels.
        /// Set to <c>false</c> for large, structurally flexible rockets (e.g. heavy launchers with
        /// flexible joints). On such vessels the measured angular velocity contains structural
        /// bending-mode oscillation that the linear term amplifies, flipping the outer-loop target
        /// velocity sign on every bending cycle and driving the structure into limit-cycle wobble.
        /// The quadratic term alone (<c>omega^2/(2*alpha)</c>) is immune to this: for typical structural
        /// oscillation (omega ~= 0.05 rad/s) it adds less than 0.1 degrees of correction regardless of gain.
        /// </summary>
        [KRPCProperty]
        public bool DecelLagCorrection {
            get { return attitudeController.DecelLagCorrection; }
            set { attitudeController.DecelLagCorrection = value; }
        }

        /// <summary>
        /// When <c>true</c>, logs one diagnostic line per physics tick to Player.log and to an
        /// in-memory buffer (see <see cref="DiagnosticLog"/>). Each line is prefixed with
        /// <c>[KRPC.AP]</c> and contains torque, MoI, angle errors, current/target angular
        /// velocity, PID gains, and control outputs. Setting to <c>true</c> also clears the
        /// buffer. Defaults to <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool DiagnosticLogging {
            get { return attitudeController.DiagnosticLogging; }
            set { attitudeController.DiagnosticLogging = value; }
        }

        /// <summary>
        /// The diagnostic log collected since <see cref="DiagnosticLogging"/> was last set to
        /// <c>true</c>. Each line corresponds to one physics tick. Returns an empty string if
        /// diagnostic logging has not been enabled or no ticks have occurred.
        /// </summary>
        [KRPCProperty]
        public string DiagnosticLog {
            get { return attitudeController.GetDiagnosticLog (); }
        }

        /// <summary>
        /// The target time to peak used to autotune the PID controllers.
        /// A vector of three times, in seconds, for each of the pitch, roll and yaw axes.
        /// Defaults to 1 second for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TimeToPeak {
            get { return attitudeController.TimeToPeak.ToTuple (); }
            set { attitudeController.TimeToPeak = value.ToVector (); }
        }

        /// <summary>
        /// The target overshoot percentage used to autotune the PID controllers.
        /// A vector of three values, between 0 and 1, for each of the pitch, roll and yaw axes.
        /// Defaults to 0.01 for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Overshoot {
            get { return attitudeController.Overshoot.ToTuple (); }
            set { attitudeController.Overshoot = value.ToVector (); }
        }

        /// <summary>
        /// Gains for the pitch PID controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically,
        /// which will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 PitchPIDGains {
            get {
                var pid = attitudeController.PitchPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (PitchPIDGains));
                attitudeController.PitchPID.SetParameters (value.Item1, value.Item2, value.Item3);
            }
        }

        /// <summary>
        /// Gains for the roll PID controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically,
        /// which will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 RollPIDGains {
            get {
                var pid = attitudeController.RollPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (RollPIDGains));
                attitudeController.RollPID.SetParameters (value.Item1, value.Item2, value.Item3);
            }
        }

        /// <summary>
        /// Gains for the yaw PID controller.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoTune"/> is true, these values are updated automatically,
        /// which will overwrite any manual changes.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 YawPIDGains {
            get {
                var pid = attitudeController.YawPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (YawPIDGains));
                attitudeController.YawPID.SetParameters (value.Item1, value.Item2, value.Item3);
            }
        }

        /// <summary>
        /// The direction vector that the SAS autopilot is trying to hold in world space.
        /// </summary>
        Vector3d SASTargetDirection ()
        {
            var vessel = InternalVessel;
            var sasMode = SASMode;

            // Stability assist
            if (sasMode == SASMode.StabilityAssist)
                throw new InvalidOperationException ("No target direction in stability assist mode");

            // Maneuver node
            if (sasMode == SASMode.Maneuver) {
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    throw new InvalidOperationException ("No maneuver node");
                var nextNode = vessel.patchedConicSolver.maneuverNodes [0];
                foreach (var node in vessel.patchedConicSolver.maneuverNodes)
                    if (node.UT < nextNode.UT)
                        nextNode = node;
                return new Node (vessel, nextNode).WorldBurnVector;
            }

            // Orbital directions, in different speed modes
            if (sasMode == SASMode.Prograde || sasMode == SASMode.Retrograde ||
                sasMode == SASMode.Normal || sasMode == SASMode.AntiNormal ||
                sasMode == SASMode.Radial || sasMode == SASMode.AntiRadial) {

                if (Control.GlobalSpeedMode == SpeedMode.Orbit) {
                    switch (sasMode) {
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
                } else if (Control.GlobalSpeedMode == SpeedMode.Surface) {
                    switch (sasMode) {
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
                } else if (Control.GlobalSpeedMode == SpeedMode.Target) {
                    switch (sasMode) {
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
            if (sasMode == SASMode.Target || sasMode == SASMode.AntiTarget) {
                var target = FlightGlobals.fetch.VesselTarget;
                if (target == null)
                    throw new InvalidOperationException ("No target");
                var direction = target.GetWorldPosition () - vessel.GetWorldPos3D ();
                if (sasMode == SASMode.AntiTarget)
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
                autoPilot.attitudeController.ReferenceFrame = ReferenceFrame.Surface (vessel);
                autoPilot.attitudeController.TargetPitch = 0;
                autoPilot.attitudeController.TargetHeading = 0;
                autoPilot.attitudeController.TargetRoll = double.NaN;
                autoPilot.Engaged = false;
                return false;
            }
            // Run the auto-pilot
            autoPilot.SAS = false;
            autoPilot.attitudeController.Update (state);
            return true;
        }
    }
}
