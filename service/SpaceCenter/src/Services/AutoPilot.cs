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
        static readonly HashSet<Guid> showInfoUI = new HashSet<Guid> ();
        readonly Guid vesselId;
        readonly AttitudeController attitudeController;
        IClient requestingClient;
        float stoppingAngleThreshold = 1f;
        float stoppingVelocityThreshold = 0.05f;

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
        /// Whether an in-game window showing the auto-pilot's state (engagement, attitude error,
        /// target, angular rate, inner-loop PID gains and oscillation suppression) is displayed for
        /// this vessel. Defaults to <c>false</c>. This is a debugging aid; the window is reset to
        /// hidden when the game is restarted.
        /// </summary>
        [KRPCProperty]
        public bool ShowInfoUI {
            get { return showInfoUI.Contains (vesselId); }
            set {
                if (value)
                    showInfoUI.Add (vesselId);
                else
                    showInfoUI.Remove (vesselId);
            }
        }

        /// <summary>
        /// Whether the info window is currently enabled for the given vessel. Used by
        /// the in-game info window addon to decide which windows to draw.
        /// </summary>
        internal static bool IsInfoUIEnabled (Guid id)
        {
            return showInfoUI.Contains (id);
        }

        /// <summary>
        /// Disables the info window for the given vessel. Called when the user closes the window
        /// in-game, so closing the window also clears <see cref="ShowInfoUI"/>.
        /// </summary>
        internal static void DisableInfoUI (Guid id)
        {
            showInfoUI.Remove (id);
        }

        /// <summary>
        /// The live, engaged auto-pilot for the given vessel, or <c>null</c> if the auto-pilot is not
        /// engaged. Unlike <see cref="Vessel.AutoPilot"/>, this returns the instance that is actually
        /// being updated each physics tick, so its state (errors, oscillation) is live.
        /// </summary>
        internal static AutoPilot GetEngaged (Guid id)
        {
            AutoPilot autoPilot;
            return engaged.TryGetValue (id, out autoPilot) ? autoPilot : null;
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
            stoppingAngleThreshold = 1f;
            stoppingVelocityThreshold = 0.05f;
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
        /// The current target pitch the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetPitch"/> while a change is slewed in; otherwise the two are equal.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetPitch {
            get { return (float)attitudeController.EffectiveTargetPitch; }
        }

        /// <summary>
        /// The current target heading the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetHeading"/> while a change is slewed in; otherwise the two are equal.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetHeading {
            get { return (float)attitudeController.EffectiveTargetHeading; }
        }

        /// <summary>
        /// The current target roll the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetRoll"/> while a change is slewed in; otherwise the two are equal.
        /// <c>NaN</c> if no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetRoll {
            get { return (float)attitudeController.EffectiveTargetRoll; }
        }

        /// <summary>
        /// Direction vector corresponding to the current target pitch and heading
        /// (see <see cref="CurrentTargetPitch"/>), in the reference frame specified by
        /// <see cref="ReferenceFrame"/>. Lags <see cref="TargetDirection"/> while a change is
        /// slewed in when <see cref="TargetSmoothingTime"/> is non-zero.
        /// </summary>
        [KRPCProperty]
        public Tuple3 CurrentTargetDirection {
            get { return attitudeController.EffectiveTargetDirection.ToTuple (); }
        }

        /// <summary>
        /// The current target rotation quaternion the auto-pilot is tracking, in the reference frame
        /// specified by <see cref="ReferenceFrame"/>. Lags <see cref="TargetRotation"/> while a
        /// change is slewed in when <see cref="TargetSmoothingTime"/> is non-zero.
        /// </summary>
        [KRPCProperty]
        public Tuple4 CurrentTargetRotation {
            get { return attitudeController.EffectiveTargetRotation.ToTuple (); }
        }

        /// <summary>
        /// Blocks until the vessel is pointing in the target direction and has
        /// the target roll (if set). Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        /// <param name="timeout">Maximum time to wait in seconds. If not specified, waits indefinitely.</param>
        [KRPCMethod]
        public void Wait (double timeout = -1)
        {
            var deadline = timeout >= 0 ? DateTime.UtcNow + TimeSpan.FromSeconds (timeout) : DateTime.MaxValue;
            WaitWithDeadline (deadline);
        }

        void WaitWithDeadline (DateTime deadline)
        {
            if (Error > stoppingAngleThreshold || InternalVessel.GetComponent<Rigidbody> ().angularVelocity.magnitude > stoppingVelocityThreshold) {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException ("AutoPilot timed out waiting to reach target direction");
                throw new YieldException<Action> (() => WaitWithDeadline (deadline));
            }
        }

        /// <summary>
        /// The threshold, in degrees, below which the pointing error must fall for
        /// <see cref="Wait"/> to return. Defaults to 1 degree.
        /// </summary>
        [KRPCProperty]
        public float StoppingAngleThreshold {
            get { return stoppingAngleThreshold; }
            set { stoppingAngleThreshold = value; }
        }

        /// <summary>
        /// The threshold angular velocity, in rad/s, below which the vessel's angular
        /// velocity magnitude must fall for <see cref="Wait"/> to return.
        /// Defaults to 0.05 rad/s.
        /// </summary>
        [KRPCProperty]
        public float StoppingVelocityThreshold {
            get { return stoppingVelocityThreshold; }
            set { stoppingVelocityThreshold = value; }
        }

        // The vessel's current attitude, decomposed into pitch/heading/roll in the AP reference frame.
        Vector3d CurrentPitchHeadingRoll ()
        {
            return ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ();
        }

        // Total pointing error (degrees) between the vessel and a target. rollControlled selects the
        // full-rotation error (honouring roll) versus the direction-only error.
        float TotalError (QuaternionD targetRotation, Vector3d targetDirection, bool rollControlled)
        {
            if (rollControlled) {
                var currentRotation = ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation);
                var rotation = targetRotation * currentRotation.Inverse ();
                double angle;
                Vector3d axis;
                GeometryExtensions.ToAngleAxis (rotation, out angle, out axis);
                return Math.Abs (GeometryExtensions.NormAngle ((float)angle));
            }
            return Math.Abs (GeometryExtensions.NormAngle (Vector3.Angle (InternalVessel.ReferenceTransform.up, ReferenceFrame.DirectionToWorldSpace (targetDirection))));
        }

        // Single-axis error (degrees) between a target angle and the vessel's current angle.
        static float AxisError (double targetAngle, double currentAngle)
        {
            return (float)Math.Abs (GeometryExtensions.ClampAngle180 (targetAngle - currentAngle));
        }

        /// <summary>
        /// The error, in degrees, between the direction the ship has been asked
        /// to point in and the direction it is pointing in. Throws an exception if the auto-pilot
        /// has not been engaged and SAS is not enabled or is in stability assist mode.
        /// </summary>
        /// <remarks>
        /// This is the error relative to the commanded target. While a change is being slewed in
        /// (see <see cref="TargetSmoothingTime"/>) it differs from <see cref="CurrentError"/>, the
        /// error relative to the target the auto-pilot is currently tracking.
        /// </remarks>
        [KRPCProperty]
        public float Error {
            get {
                if (Engaged)
                    return TotalError (attitudeController.TargetRotation, attitudeController.TargetDirection, !double.IsNaN (attitudeController.TargetRoll));
                if (SAS && SASMode != SASMode.StabilityAssist)
                    return Math.Abs (GeometryExtensions.NormAngle (Vector3.Angle (InternalVessel.ReferenceTransform.up, SASTargetDirection ())));
                throw new InvalidOperationException ("The auto-pilot is not engaged");
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
                return AxisError (attitudeController.TargetPitch, CurrentPitchHeadingRoll ().x);
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
                return AxisError (attitudeController.TargetHeading, CurrentPitchHeadingRoll ().y);
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
                return AxisError (attitudeController.TargetRoll, CurrentPitchHeadingRoll ().z);
            }
        }

        /// <summary>
        /// The error, in degrees, between the direction the auto-pilot is currently tracking and the
        /// direction the ship is pointing in. Unlike <see cref="Error"/> (which is relative to the
        /// commanded target), this is relative to the slewed target the auto-pilot is currently
        /// holding, so it stays small while a smoothed change (see <see cref="TargetSmoothingTime"/>)
        /// is fed in. Equal to <see cref="Error"/> when smoothing is off. Throws an exception if the
        /// auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public float CurrentError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                return TotalError (attitudeController.EffectiveTargetRotation, attitudeController.EffectiveTargetDirection, !double.IsNaN (attitudeController.EffectiveTargetRoll));
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current pitch and the pitch the auto-pilot is
        /// currently tracking (see <see cref="CurrentTargetPitch"/>). Throws an exception if the
        /// auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public float CurrentPitchError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                return AxisError (attitudeController.EffectiveTargetPitch, CurrentPitchHeadingRoll ().x);
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current heading and the heading the auto-pilot
        /// is currently tracking (see <see cref="CurrentTargetHeading"/>). Throws an exception if the
        /// auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public float CurrentHeadingError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                return AxisError (attitudeController.EffectiveTargetHeading, CurrentPitchHeadingRoll ().y);
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current roll and the roll the auto-pilot is
        /// currently tracking (see <see cref="CurrentTargetRoll"/>). Throws an exception if the
        /// auto-pilot has not been engaged or no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float CurrentRollError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                if (double.IsNaN (attitudeController.EffectiveTargetRoll))
                    throw new InvalidOperationException ("No target roll has been set");
                return AxisError (attitudeController.EffectiveTargetRoll, CurrentPitchHeadingRoll ().z);
            }
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
        /// The angle, in degrees, at which the autopilot considers the vessel to be pointing close
        /// to the target direction. This determines the midpoint of the pitch/yaw target velocity
        /// attenuation function. Pitch and yaw are controlled jointly, so a single angle applies to
        /// both. Defaults to 1°.
        /// </summary>
        [KRPCProperty]
        public double PitchYawAttenuationAngle {
            get { return attitudeController.PitchYawAttenuationAngle; }
            set { attitudeController.PitchYawAttenuationAngle = value; }
        }

        /// <summary>
        /// The angle, in degrees, at which the autopilot considers the vessel to be pointing close
        /// to the target roll. This determines the midpoint of the roll-axis target velocity
        /// attenuation function. Defaults to 1°.
        /// </summary>
        [KRPCProperty]
        public double RollAttenuationAngle {
            get { return attitudeController.RollAttenuationAngle; }
            set { attitudeController.RollAttenuationAngle = value; }
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
        /// The duration, in seconds, over which the control output is faded in when the
        /// autopilot is engaged. This soft-start spreads the engagement transient over many
        /// physics ticks so engaging (on the pad or mid-flight) does not command a near-maximum
        /// control deflection that can excite an oscillation. Defaults to 0.5 seconds.
        /// Set to 0 to disable the fade-in.
        /// </summary>
        [KRPCProperty]
        public double SoftStartTime {
            get { return attitudeController.SoftStartTime; }
            set { attitudeController.SoftStartTime = value; }
        }

        /// <summary>
        /// The duration, in seconds, over which a change to the target attitude is applied to the
        /// control target. When set above zero, changing the target pitch, heading, roll, direction
        /// or rotation makes the effective control target ramp smoothly (a constant-rate rotation)
        /// from its current value to the new value over this many seconds, rather than jumping
        /// instantly. This lets a slow control loop drive a smooth maneuver (for example a gravity
        /// turn) without inducing oscillation from stepwise target changes. Defaults to 0
        /// (instantaneous).
        /// </summary>
        [KRPCProperty]
        public double TargetSmoothingTime {
            get { return attitudeController.TargetSmoothingTime; }
            set { attitudeController.TargetSmoothingTime = value; }
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
        /// Controls how the auto-pilot suppresses structural oscillation (wobble) on the pitch and
        /// yaw axes of a structurally flexible vessel. When <see cref="OscillationControl.Automatic"/>
        /// (the default) the auto-pilot detects the oscillation at runtime, estimates its frequency
        /// and routes it to the appropriate tool (a notch filter for a low-frequency mode near the
        /// control band, a low-pass for a high-frequency mode). <see cref="OscillationControl.Off"/>
        /// disables suppression, giving full control authority at the cost of allowing the vessel to
        /// wobble. <see cref="OscillationControl.Notch"/> and <see cref="OscillationControl.LowPass"/>
        /// force the respective tool unconditionally at <see cref="PitchYawOscillationFrequency"/>,
        /// for a vessel known in advance to be flexible.
        /// </summary>
        [KRPCProperty]
        public OscillationControl PitchYawOscillationControl {
            get { return attitudeController.PitchYawOscillationControl; }
            set { attitudeController.PitchYawOscillationControl = value; }
        }

        /// <summary>
        /// Controls how the auto-pilot suppresses structural oscillation (wobble) on the roll axis.
        /// Behaves as <see cref="PitchYawOscillationControl"/> but for roll, using
        /// <see cref="RollOscillationFrequency"/>. Defaults to
        /// <see cref="OscillationControl.Automatic"/>.
        /// </summary>
        [KRPCProperty]
        public OscillationControl RollOscillationControl {
            get { return attitudeController.RollOscillationControl; }
            set { attitudeController.RollOscillationControl = value; }
        }

        /// <summary>
        /// The structural mode frequency, in Hz, for the pitch/yaw axis group. Used directly as the
        /// filter frequency in <see cref="OscillationControl.Notch"/> / <see cref="OscillationControl.LowPass"/>
        /// mode, and as the seed for the automatic frequency estimator before it acquires. Defaults
        /// to 1.5 Hz.
        /// </summary>
        [KRPCProperty]
        public double PitchYawOscillationFrequency {
            get { return attitudeController.PitchYawOscillationFrequency; }
            set { attitudeController.PitchYawOscillationFrequency = value; }
        }

        /// <summary>
        /// The structural mode frequency, in Hz, for the roll axis. Behaves as
        /// <see cref="PitchYawOscillationFrequency"/> but for roll. Defaults to 1.5 Hz.
        /// </summary>
        [KRPCProperty]
        public double RollOscillationFrequency {
            get { return attitudeController.RollOscillationFrequency; }
            set { attitudeController.RollOscillationFrequency = value; }
        }

        /// <summary>
        /// The quality factor of the notch filter used to suppress a low-frequency structural mode.
        /// A higher value gives a narrower notch (less in-band control lag but less tolerance to the
        /// mode frequency drifting); a lower value gives a wider notch. Defaults to 2.5. This is an
        /// advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationNotchQ {
            get { return attitudeController.OscillationNotchQ; }
            set { attitudeController.OscillationNotchQ = value; }
        }

        /// <summary>
        /// The inner control loop bandwidth, in rad/s, that an axis is reduced towards while
        /// suppression is engaged on it (a structurally flexible axis). Lowering it suppresses
        /// oscillation more strongly; raising it keeps more control authority at the cost of allowing
        /// more wobble. Defaults to 1 rad/s. This is an advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationBandwidthFloor {
            get { return attitudeController.OscillationBandwidthFloor; }
            set { attitudeController.OscillationBandwidthFloor = value; }
        }

        /// <summary>
        /// The sensitivity of the automatic oscillation detector, as a multiple of the
        /// physically-achievable change in angular velocity per physics tick. A tick-to-tick change
        /// in the measured rate larger than this is treated as structural oscillation rather than a
        /// control response. Lowering it makes detection more sensitive. Defaults to 4. This is an
        /// advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationDetectionThreshold {
            get { return attitudeController.OscillationDetectionThreshold; }
            set { attitudeController.OscillationDetectionThreshold = value; }
        }

        /// <summary>
        /// A manual override, between 0 and 1 for each of the pitch, roll and yaw axes, that forces the
        /// flexible-craft oscillation mitigation (reduced control loop bandwidth, feedforward cut and
        /// rate-independent target) on for a structurally flexible (latched) axis, regardless of how
        /// far the vessel is pointing from the target. 0 (the default) applies no override, leaving the
        /// mitigation to engage automatically only while holding. This is an advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public Tuple3 OscillationControlLevel {
            get { return attitudeController.OscillationControlLevel.ToTuple (); }
            set { attitudeController.OscillationControlLevel = value.ToVector (); }
        }

        /// <summary>
        /// The amplitude of control-output oscillation, measured about its slowly-varying trim, above
        /// which a structurally flexible (latched) axis is treated as still limit-cycling and the
        /// oscillation mitigation is engaged automatically regardless of pointing error. Lowering it
        /// makes the automatic engagement more sensitive. Defaults to 0.2. Compare against
        /// <see cref="PitchYawControlOscillation"/> / <see cref="RollControlOscillation"/>. This is an
        /// advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationControlThreshold {
            get { return attitudeController.OscillationControlThreshold; }
            set { attitudeController.OscillationControlThreshold = value; }
        }

        /// <summary>
        /// The current amplitude of control-output oscillation on the pitch/yaw axis group, measured as
        /// the deviation of the delivered control about its slowly-varying trim. A settled hold sits
        /// near zero; a sustained limit cycle drives it toward 1. Read-only. See
        /// <see cref="OscillationControlThreshold"/>.
        /// </summary>
        [KRPCProperty]
        public double PitchYawControlOscillation {
            get { return attitudeController.PitchYawControlOscillation; }
        }

        /// <summary>
        /// The current amplitude of control-output oscillation on the roll axis, measured as the
        /// deviation of the delivered control about its slowly-varying trim. Read-only. See
        /// <see cref="PitchYawControlOscillation"/>.
        /// </summary>
        [KRPCProperty]
        public double RollControlOscillation {
            get { return attitudeController.RollControlOscillation; }
        }

        /// <summary>
        /// A measure, between 0 and 1 for each of the pitch, roll and yaw axes, of how strongly the
        /// auto-pilot currently detects structural oscillation (wobble) on that axis. 0 means none
        /// detected; values approaching 1 mean a sustained structural oscillation. Read-only.
        /// </summary>
        [KRPCProperty]
        public Tuple3 OscillationLevel {
            get { return attitudeController.OscillationLevel.ToTuple (); }
        }

        /// <summary>
        /// Whether the auto-pilot has confirmed the pitch/yaw axes to be structurally flexible and
        /// latched oscillation suppression on for them. Read-only. See
        /// <see cref="PitchYawOscillationControl"/>.
        /// </summary>
        [KRPCProperty]
        public bool PitchYawOscillationLatched {
            get { return attitudeController.PitchYawOscillationLatched; }
        }

        /// <summary>
        /// Whether the auto-pilot has confirmed the roll axis to be structurally flexible and latched
        /// oscillation suppression on for it. Read-only. See <see cref="RollOscillationControl"/>.
        /// </summary>
        [KRPCProperty]
        public bool RollOscillationLatched {
            get { return attitudeController.RollOscillationLatched; }
        }

        /// <summary>
        /// The structural oscillation frequency, in Hz, estimated by the automatic detector for the
        /// pitch/yaw axis group, or <c>NaN</c> until the estimator acquires. The estimator runs in
        /// all modes, so this is observable even when suppression is off or forced. Read-only.
        /// </summary>
        [KRPCProperty]
        public double PitchYawOscillationDetectedFrequency {
            get { return attitudeController.PitchYawOscillationDetectedFrequency; }
        }

        /// <summary>
        /// The structural oscillation frequency, in Hz, estimated by the automatic detector for the
        /// roll axis, or <c>NaN</c> until the estimator acquires. Read-only. See
        /// <see cref="PitchYawOscillationDetectedFrequency"/>.
        /// </summary>
        [KRPCProperty]
        public double RollOscillationDetectedFrequency {
            get { return attitudeController.RollOscillationDetectedFrequency; }
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
        /// Measured angular velocity (raw, roll-invariant frame, rad/s) for the in-game info window.
        /// </summary>
        internal Tuple3 MeasuredAngularVelocity {
            get { return attitudeController.MeasuredAngularVelocity.ToTuple (); }
        }

        /// <summary>
        /// Target angular velocity the inner loop is tracking (roll-invariant frame, rad/s) for the
        /// in-game info window.
        /// </summary>
        internal Tuple3 TargetAngularVelocity {
            get { return attitudeController.TargetAngularVelocity.ToTuple (); }
        }

        /// <summary>
        /// Per-axis hold-gated mitigation weight in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 MitigationLevel {
            get { return attitudeController.MitigationLevel.ToTuple (); }
        }

        /// <summary>
        /// Active suppression tool on an axis (0 none, 1 notch, 2 low-pass) for the in-game info
        /// window.
        /// </summary>
        internal int ActiveSuppressionTool (int axis)
        {
            return attitudeController.ActiveSuppressionTool (axis);
        }

        /// <summary>
        /// The oscillation level at which an axis latches as flexible, for the in-game info window.
        /// </summary>
        internal double OscillationLatchThreshold {
            get { return attitudeController.OscillationLatchThreshold; }
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
