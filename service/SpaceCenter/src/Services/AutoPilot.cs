using System;
using System.Collections.Generic;
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
    /// the auto-pilot will be disengaged. Its configuration and target are left unchanged.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class AutoPilot : Equatable<AutoPilot>
    {
        static readonly HashSet<Guid> showInfoUI = new HashSet<Guid> ();
        readonly Guid vesselId;

        internal AutoPilot (global::Vessel vessel)
        {
            vesselId = vessel.id;
        }

        /// <summary>
        /// The vessel's attitude controller.
        /// </summary>
        AttitudeController Controller {
            get { return PilotAddon.GetAttitudeController (InternalVessel); }
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
            get { return GetSAS (InternalVessel); }
            set {
                if (value && Engaged)
                    throw new InvalidOperationException ("SAS cannot be enabled when the auto-pilot is engaged");
                SetSAS (InternalVessel, value);
            }
        }

        internal static bool GetSAS (global::Vessel vessel)
        {
            return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)];
        }

        internal static void SetSAS (global::Vessel vessel, bool value)
        {
            vessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value);
        }

        /// <summary>
        /// The current <see cref="SASMode"/>.
        /// These modes are equivalent to the mode buttons to the left of the navball that appear
        /// when SAS is enabled.
        /// </summary>
        /// <remarks>Equivalent to <see cref="Control.SASMode"/></remarks>
        [KRPCProperty]
        public SASMode SASMode {
            get { return GetSASMode (InternalVessel); }
            set { SetSASMode (InternalVessel, value); }
        }

        internal static SASMode GetSASMode (global::Vessel vessel)
        {
            return vessel.Autopilot.Mode.ToSASMode ();
        }

        internal static void SetSASMode (global::Vessel vessel, SASMode value)
        {
            var autopilot = vessel.Autopilot;
            var mode = value.FromSASMode ();
            if (!autopilot.CanSetMode (mode))
                throw new InvalidOperationException ("Cannot set SAS mode of vessel");
            // When SAS is enabled and the mode is set in the same physics tick,
            // VesselAutopilot.Update has not yet enabled the autopilot, so
            // SetMode would only store the mode. The autopilot then enables with
            // Enable(StabilityAssist) on the next update, discarding the mode.
            // Enable atomically with the requested mode to avoid this.
            if (!autopilot.Enabled && GetSAS (vessel))
                autopilot.Enable (mode);
            else
                autopilot.SetMode (mode);
            // Update the UI buttons
            var modeIndex = (int)autopilot.Mode;
            var modeButtons = UnityEngine.Object.FindObjectOfType<VesselAutopilotUI> ().modeButtons;
            modeButtons [modeIndex].SetState (true);
        }

        /// <summary>
        /// Whether the auto-pilot is engaged.
        /// Setting to <c>true</c> engages the auto-pilot; setting to <c>false</c> disengages it.
        /// </summary>
        [KRPCProperty]
        public bool Engaged {
            get { return Controller.Engaged; }
            set {
                if (value)
                    Controller.Engage (CallContext.Client);
                else
                    Controller.Disengage ();
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
        /// An auto-pilot object for the given vessel, or <c>null</c> if the vessel's auto-pilot
        /// is not engaged (or the vessel no longer exists). Used by the in-game info window;
        /// the returned object is a view onto the vessel's live attitude controller.
        /// </summary>
        internal static AutoPilot GetEngaged (Guid id)
        {
            var controller = PilotAddon.FindAttitudeController (id);
            if (controller == null || !controller.Engaged)
                return null;
            try {
                return new AutoPilot (FlightGlobalsExtensions.GetVesselById (id));
            } catch (ArgumentException) {
                return null;
            }
        }

        /// <summary>
        /// Disengages the auto-pilot and resets all configuration parameters to their defaults.
        /// Also resets the target pitch, heading and roll, and clears all internal controller
        /// state, including the oscillation detector's structural level — which otherwise
        /// persists across engagements so that a craft known to be flexible re-latches quickly.
        /// </summary>
        [KRPCMethod]
        public void Reset ()
        {
            Controller.Reset ();
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
            get { return Controller.ReferenceFrame; }
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
                Controller.ReferenceFrame = value;
            }
        }

        /// <summary>
        /// The target pitch, in degrees, between -90° and +90°.
        /// </summary>
        /// <remarks>
        /// A convenience for aiming the nose by angle. Heading (and hence roll) is ill-defined when
        /// the nose is near vertical (pitch → ±90°); near the vertical prefer
        /// <see cref="TargetDirection"/> or <see cref="SetDirectionAndUp"/>. The setter preserves the
        /// current roll relative to <see cref="UpReference"/>.
        /// </remarks>
        [KRPCProperty]
        public float TargetPitch {
            get { return (float)Controller.TargetPitch; }
            set { Controller.TargetPitch = value; }
        }

        /// <summary>
        /// The target heading, in degrees, between 0° and 360°.
        /// </summary>
        /// <remarks>
        /// A convenience for aiming the nose by angle, ill-defined when the nose is near vertical
        /// (pitch → ±90°) — see <see cref="TargetPitch"/>. The setter preserves the current roll
        /// relative to <see cref="UpReference"/>.
        /// </remarks>
        [KRPCProperty]
        public float TargetHeading {
            get { return (float)Controller.TargetHeading; }
            set { Controller.TargetHeading = value; }
        }

        /// <summary>
        /// The target roll, in degrees, measured about the vessel's nose relative to the
        /// <see cref="UpReference"/> (roll 0 aligns the vessel's dorsal/roof axis with the reference;
        /// positive roll banks right). <c>NaN</c> if no target roll is set.
        /// </summary>
        /// <remarks>
        /// When left unset (<c>NaN</c>) the auto-pilot suppresses roll rotation — it drives the roll
        /// rate to zero rather than holding a specific roll angle. Setting a value re-rolls the
        /// current target to that angle relative to the up reference while keeping the nose direction.
        /// With the default reference (the frame's up) this reproduces the historical roll away from
        /// the vertical, and is ill-defined only when the nose points along the reference (near
        /// straight up or down). To hold a well-defined roll through the vertical — for example a
        /// gravity turn — set the up reference off the flight path (see
        /// <see cref="SetDirectionAndUp"/> / <see cref="UpReference"/>).
        /// </remarks>
        [KRPCProperty]
        public float TargetRoll {
            get { return (float)Controller.TargetRoll; }
            set { Controller.TargetRoll = value; }
        }

        /// <summary>
        /// The reference direction, in the reference frame specified by <see cref="ReferenceFrame"/>,
        /// that <see cref="TargetRoll"/> is measured against: at roll 0 the vessel's dorsal (roof)
        /// axis is aligned with this vector's component perpendicular to the nose. Defaults to the
        /// frame's up (the zenith / radial-out direction).
        /// </summary>
        /// <remarks>
        /// Setting this re-anchors how roll is measured without moving the current target, so
        /// the reference can be set once and then rolls commanded against it with
        /// <see cref="TargetRoll"/> while the nose direction changes freely. It is also set as a side
        /// effect of <see cref="SetDirectionAndUp"/>. Setting the target rotation, target direction,
        /// or the scalar pitch/heading leaves it unchanged. Choosing a reference off the flight path
        /// keeps roll well-defined through the vertical.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 UpReference {
            get { return Controller.UpReference.ToTuple (); }
            set { Controller.UpReference = value.ToVector (); }
        }

        /// <summary>
        /// Set target pitch and heading angles.
        /// </summary>
        /// <param name="pitch">Target pitch angle, in degrees between -90° and +90°.</param>
        /// <param name="heading">Target heading angle, in degrees between 0° and 360°.</param>
        /// <remarks>
        /// A convenience for aiming the nose by angle; heading is ill-defined when the nose is near
        /// vertical (pitch → ±90°), so near the vertical prefer <see cref="TargetDirection"/> or
        /// <see cref="SetDirectionAndUp"/>. Preserves the current roll relative to
        /// <see cref="UpReference"/>.
        /// </remarks>
        [KRPCMethod]
        public void TargetPitchAndHeading (float pitch, float heading)
        {
            Controller.TargetPitch = pitch;
            Controller.TargetHeading = heading;
        }

        /// <summary>
        /// Set the target attitude from a nose direction and an up vector: point the nose along
        /// <paramref name="direction"/> and roll so the vessel's dorsal (roof) axis aligns with
        /// <paramref name="up"/> (its component perpendicular to the nose), then apply an optional
        /// <paramref name="roll"/> offset about the nose. Both vectors are in the reference frame
        /// specified by <see cref="ReferenceFrame"/>.
        /// </summary>
        /// <param name="direction">The direction to point the nose in.</param>
        /// <param name="up">The reference direction the roof is rolled towards. Need not be
        /// normalized or perpendicular to <paramref name="direction"/> — its component perpendicular
        /// to the nose is used. Stored as the <see cref="UpReference"/>.</param>
        /// <param name="roll">An additional roll about the nose, in degrees (positive banks right).
        /// Defaults to 0.</param>
        /// <remarks>
        /// This is the way to hold a well-defined orientation through a maneuver — for example a
        /// gravity turn: pass a fixed <paramref name="up"/> (say north) and the roll stays defined the
        /// whole way, with no singularity at the vertical. It is well-defined for every nose direction
        /// except <paramref name="up"/> parallel to <paramref name="direction"/> (asking the roof to
        /// point where the nose already points), where it falls back to pointing the nose only.
        /// Equivalent to setting <see cref="UpReference"/> to <paramref name="up"/>, aiming at
        /// <paramref name="direction"/> and setting <see cref="TargetRoll"/> to
        /// <paramref name="roll"/>.
        /// </remarks>
        [KRPCMethod]
        public void SetDirectionAndUp (Tuple3 direction, Tuple3 up, float roll = 0)
        {
            Controller.SetTargetDirectionAndUp (direction.ToVector (), up.ToVector (), roll);
        }

        /// <summary>
        /// Direction vector corresponding to the target pitch and heading.
        /// This is in the reference frame specified by <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TargetDirection {
            get { return Controller.TargetDirection.ToTuple (); }
            set { Controller.SetTargetDirection (value.ToVector ()); }
        }

        /// <summary>
        /// The target rotation quaternion. Setting this also sets the target roll.
        /// This is in the reference frame specified by <see cref="ReferenceFrame"/>.
        /// </summary>
        [KRPCProperty]
        public Tuple4 TargetRotation {
            get { return Controller.TargetRotation.ToTuple (); }
            set { Controller.SetTargetRotation (value.ToQuaternion ()); }
        }

        /// <summary>
        /// The current target pitch the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetPitch"/> while a change is slewed in; otherwise the two are equal.
        /// A convenience scalar, ill-defined near the vertical — see <see cref="TargetPitch"/>.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetPitch {
            get { return (float)Controller.EffectiveTargetPitch; }
        }

        /// <summary>
        /// The current target heading the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetHeading"/> while a change is slewed in; otherwise the two are equal.
        /// A convenience scalar, ill-defined near the vertical — see <see cref="TargetHeading"/>.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetHeading {
            get { return (float)Controller.EffectiveTargetHeading; }
        }

        /// <summary>
        /// The current target roll the auto-pilot is tracking, in degrees. When
        /// <see cref="TargetSmoothingTime"/> is non-zero this lags the commanded
        /// <see cref="TargetRoll"/> while a change is slewed in; otherwise the two are equal.
        /// <c>NaN</c> if no target roll is set.
        /// </summary>
        [KRPCProperty]
        public float CurrentTargetRoll {
            get { return (float)Controller.EffectiveTargetRoll; }
        }

        /// <summary>
        /// Direction vector corresponding to the current target pitch and heading
        /// (see <see cref="CurrentTargetPitch"/>), in the reference frame specified by
        /// <see cref="ReferenceFrame"/>. Lags <see cref="TargetDirection"/> while a change is
        /// slewed in when <see cref="TargetSmoothingTime"/> is non-zero.
        /// </summary>
        [KRPCProperty]
        public Tuple3 CurrentTargetDirection {
            get { return Controller.EffectiveTargetDirection.ToTuple (); }
        }

        /// <summary>
        /// The current target rotation quaternion the auto-pilot is tracking, in the reference frame
        /// specified by <see cref="ReferenceFrame"/>. Lags <see cref="TargetRotation"/> while a
        /// change is slewed in when <see cref="TargetSmoothingTime"/> is non-zero.
        /// </summary>
        [KRPCProperty]
        public Tuple4 CurrentTargetRotation {
            get { return Controller.EffectiveTargetRotation.ToTuple (); }
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
            if (Error > Controller.StoppingAngleThreshold ||
                InternalVessel.GetComponent<Rigidbody> ().angularVelocity.magnitude > Controller.StoppingVelocityThreshold) {
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
            get { return Controller.StoppingAngleThreshold; }
            set { Controller.StoppingAngleThreshold = value; }
        }

        /// <summary>
        /// The threshold angular velocity, in rad/s, below which the vessel's angular
        /// velocity magnitude must fall for <see cref="Wait"/> to return.
        /// Defaults to 0.05 rad/s.
        /// </summary>
        [KRPCProperty]
        public float StoppingVelocityThreshold {
            get { return Controller.StoppingVelocityThreshold; }
            set { Controller.StoppingVelocityThreshold = value; }
        }

        // The vessel's current attitude, decomposed into pitch/heading/roll in the AP reference frame.
        Vector3d CurrentPitchHeadingRoll ()
        {
            return ReferenceFrame.RotationFromWorldSpace (InternalVessel.ReferenceTransform.rotation).PitchHeadingRoll ();
        }

        // The vessel's current pitch (degrees) in the AP reference frame. Used by the info window to
        // scale the heading-error tolerance near the vertical singularity; not part of the RPC surface.
        internal float CurrentPitch {
            get { return (float)CurrentPitchHeadingRoll ().x; }
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
                    return TotalError (Controller.TargetRotation, Controller.TargetDirection, !double.IsNaN (Controller.TargetRoll));
                if (SAS && SASMode != SASMode.StabilityAssist)
                    return Math.Abs (GeometryExtensions.NormAngle (Vector3.Angle (InternalVessel.ReferenceTransform.up, SASTargetDirection ())));
                throw new InvalidOperationException ("The auto-pilot is not engaged");
            }
        }

        /// <summary>
        /// The per-axis attitude error (pitch, yaw, roll), in degrees, between the vessel's current
        /// attitude and the commanded target. All three components come from one singularity-free
        /// residual decomposition, so they stay well-defined near the vertical (unlike a subtraction
        /// of pitch/heading/roll angles). The scalar <see cref="PitchError"/>,
        /// <see cref="HeadingError"/> and <see cref="RollError"/> are the magnitudes of the pitch, yaw
        /// and roll components respectively. Throws an exception if the auto-pilot has not been
        /// engaged.
        /// </summary>
        [KRPCProperty]
        public Tuple3 AttitudeError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                return Controller.AttitudeErrorTo (
                    Controller.TargetRotation, Controller.TargetDirection).ToTuple ();
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target pitch.
        /// Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        /// <remarks>
        /// The pitch component of <see cref="AttitudeError"/> — the pitch part of the direction error
        /// resolved in the roll-invariant frame, well-defined near the vertical.
        /// </remarks>
        [KRPCProperty]
        public float PitchError {
            get { return (float)Math.Abs (AttitudeError.Item1); }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target heading.
        /// Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        /// <remarks>
        /// The yaw component of <see cref="AttitudeError"/> — the yaw part of the direction error
        /// resolved in the roll-invariant frame, well-defined near the vertical (unlike the absolute
        /// heading, which is undefined at the pole).
        /// </remarks>
        [KRPCProperty]
        public float HeadingError {
            get { return (float)Math.Abs (AttitudeError.Item2); }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current and target roll.
        /// Throws an exception if the auto-pilot has not been engaged or no target roll is set.
        /// </summary>
        /// <remarks>
        /// Measured about the vessel's nose axis, so it stays well-defined near the vertical
        /// singularity — unlike a subtraction of pitch/heading/roll angles, whose roll term is
        /// ill-conditioned when the vessel points close to straight up or down.
        /// </remarks>
        [KRPCProperty]
        public float RollError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                if (double.IsNaN (Controller.TargetRoll))
                    throw new InvalidOperationException ("No target roll has been set");
                return (float)Controller.RollErrorTo (
                    Controller.TargetRotation, Controller.TargetDirection);
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
                return TotalError (Controller.EffectiveTargetRotation, Controller.EffectiveTargetDirection, !double.IsNaN (Controller.EffectiveTargetRoll));
            }
        }

        /// <summary>
        /// The per-axis attitude error (pitch, yaw, roll), in degrees, between the vessel's current
        /// attitude and the target the auto-pilot is currently tracking (the slewed target — see
        /// <see cref="CurrentTargetRotation"/>). Like <see cref="AttitudeError"/> but relative to the
        /// current target, so it stays small while a smoothed change (see
        /// <see cref="TargetSmoothingTime"/>) is fed in; equal to <see cref="AttitudeError"/> when
        /// smoothing is off. Throws an exception if the auto-pilot has not been engaged.
        /// </summary>
        [KRPCProperty]
        public Tuple3 CurrentAttitudeError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                return Controller.AttitudeErrorTo (
                    Controller.EffectiveTargetRotation,
                    Controller.EffectiveTargetDirection).ToTuple ();
            }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current pitch and the pitch the auto-pilot is
        /// currently tracking (see <see cref="CurrentTargetPitch"/>). Throws an exception if the
        /// auto-pilot has not been engaged.
        /// </summary>
        /// <remarks>
        /// The pitch component of <see cref="CurrentAttitudeError"/>, well-defined near the vertical.
        /// </remarks>
        [KRPCProperty]
        public float CurrentPitchError {
            get { return (float)Math.Abs (CurrentAttitudeError.Item1); }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current heading and the heading the auto-pilot
        /// is currently tracking (see <see cref="CurrentTargetHeading"/>). Throws an exception if the
        /// auto-pilot has not been engaged.
        /// </summary>
        /// <remarks>
        /// The yaw component of <see cref="CurrentAttitudeError"/>, well-defined near the vertical.
        /// </remarks>
        [KRPCProperty]
        public float CurrentHeadingError {
            get { return (float)Math.Abs (CurrentAttitudeError.Item2); }
        }

        /// <summary>
        /// The error, in degrees, between the vessels current roll and the roll the auto-pilot is
        /// currently tracking (see <see cref="CurrentTargetRoll"/>). Throws an exception if the
        /// auto-pilot has not been engaged or no target roll is set.
        /// </summary>
        /// <remarks>
        /// Measured about the vessel's nose axis, so it stays well-defined near the vertical
        /// singularity — see <see cref="RollError"/>.
        /// </remarks>
        [KRPCProperty]
        public float CurrentRollError {
            get {
                if (!Engaged)
                    throw new InvalidOperationException ("The auto-pilot is not engaged");
                if (double.IsNaN (Controller.EffectiveTargetRoll))
                    throw new InvalidOperationException ("No target roll has been set");
                return (float)Controller.RollErrorTo (
                    Controller.EffectiveTargetRotation, Controller.EffectiveTargetDirection);
            }
        }

        /// <summary>
        /// The direction error, in degrees, above which roll blending is fully suppressed.
        /// Defaults to 20 degrees.
        /// </summary>
        [KRPCProperty]
        public double RollStartAngle {
            get { return Controller.RollStartAngle; }
            set { Controller.RollStartAngle = value; }
        }

        /// <summary>
        /// The direction error, in degrees, below which roll is fully engaged.
        /// Roll blends linearly between <see cref="RollStartAngle"/> and this value.
        /// Defaults to 15 degrees.
        /// </summary>
        [KRPCProperty]
        public double RollEngageAngle {
            get { return Controller.RollEngageAngle; }
            set { Controller.RollEngageAngle = value; }
        }

        /// <summary>
        /// The maximum angular velocity of the vessel, in rad/s, for each of the pitch, roll
        /// and yaw axes. Limits the target angular velocity computed by the bang-bang profile so
        /// that vessels with very high torque availability do not spin faster than desired.
        /// Defaults to 1 rad/s for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 MaxAngularVelocity {
            get { return Controller.MaxAngularVelocity.ToTuple (); }
            set { Controller.MaxAngularVelocity = value.ToVector (); }
        }

        /// <summary>
        /// The angle, in degrees, at which the autopilot considers the vessel to be pointing close
        /// to the target direction. This sets the high angle of the pitch/yaw pointing deadband: at
        /// or above this error the target velocity is at full, and below it the target velocity
        /// ramps linearly to zero at half this angle, so the vessel coasts to a stop. Pitch and yaw
        /// are controlled jointly, so a single angle applies to both. Defaults to 1°.
        /// </summary>
        [KRPCProperty]
        public double PitchYawAttenuationAngle {
            get { return Controller.PitchYawAttenuationAngle; }
            set { Controller.PitchYawAttenuationAngle = value; }
        }

        /// <summary>
        /// The angle, in degrees, at which the autopilot considers the vessel to be pointing close
        /// to the target roll. This sets the high angle of the roll-axis pointing deadband: at or
        /// above this error the target velocity is at full, and below it the target velocity ramps
        /// linearly to zero at half this angle, so the roll coasts to a stop. Defaults to 1°.
        /// </summary>
        [KRPCProperty]
        public double RollAttenuationAngle {
            get { return Controller.RollAttenuationAngle; }
            set { Controller.RollAttenuationAngle = value; }
        }

        /// <summary>
        /// Whether the rotation rate controllers PID parameters should be automatically tuned
        /// using the vessels moment of inertia and available torque. Defaults to <c>true</c>.
        /// See <see cref="TimeToPeak"/> and <see cref="Overshoot"/>.
        /// </summary>
        [KRPCProperty]
        public bool AutoTune {
            get { return Controller.AutoTune; }
            set { Controller.AutoTune = value; }
        }

        /// <summary>
        /// The target time to peak used to autotune the PID controllers.
        /// A vector of three times, in seconds, for each of the pitch, roll and yaw axes.
        /// Defaults to 1 second for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 TimeToPeak {
            get { return Controller.TimeToPeak.ToTuple (); }
            set { Controller.TimeToPeak = value.ToVector (); }
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
            get { return Controller.SoftStartTime; }
            set { Controller.SoftStartTime = value; }
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
            get { return Controller.TargetSmoothingTime; }
            set { Controller.TargetSmoothingTime = value; }
        }

        /// <summary>
        /// The target overshoot percentage used to autotune the PID controllers.
        /// A vector of three values, between 0 and 1, for each of the pitch, roll and yaw axes.
        /// Defaults to 0.01 for each axis.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Overshoot {
            get { return Controller.Overshoot.ToTuple (); }
            set { Controller.Overshoot = value.ToVector (); }
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
                var pid = Controller.PitchPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (PitchPIDGains));
                Controller.PitchPID.SetParameters (value.Item1, value.Item2, value.Item3);
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
                var pid = Controller.RollPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (RollPIDGains));
                Controller.RollPID.SetParameters (value.Item1, value.Item2, value.Item3);
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
                var pid = Controller.YawPID;
                return new Tuple3 (pid.Kp, pid.Ki, pid.Kd);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (YawPIDGains));
                Controller.YawPID.SetParameters (value.Item1, value.Item2, value.Item3);
            }
        }

        /// <summary>
        /// Controls the rate-feedback filtering (the wobble-suppression filter on the measured
        /// angular velocity) for the pitch and yaw axes of a structurally flexible vessel. When
        /// <see cref="RateFilterMode.Automatic"/> (the default) the auto-pilot detects the
        /// oscillation at runtime, estimates its frequency and routes it to the appropriate tool
        /// (a notch filter for a low-frequency mode near the control band, a low-pass for a
        /// high-frequency mode). <see cref="RateFilterMode.Off"/> disables rate filtering only —
        /// the other oscillation mitigations are unaffected. <see cref="RateFilterMode.Notch"/>
        /// and <see cref="RateFilterMode.LowPass"/> force the respective tool unconditionally at
        /// <see cref="PitchYawOscillationFrequency"/>, for a vessel known in advance to be
        /// flexible.
        /// </summary>
        [KRPCProperty]
        public RateFilterMode PitchYawRateFilterMode {
            get { return Controller.PitchYawRateFilterMode; }
            set { Controller.PitchYawRateFilterMode = value; }
        }

        /// <summary>
        /// Controls the rate-feedback filtering for the roll axis. Behaves as
        /// <see cref="PitchYawRateFilterMode"/> but for roll, using
        /// <see cref="RollOscillationFrequency"/>. Defaults to
        /// <see cref="RateFilterMode.Automatic"/>.
        /// </summary>
        [KRPCProperty]
        public RateFilterMode RollRateFilterMode {
            get { return Controller.RollRateFilterMode; }
            set { Controller.RollRateFilterMode = value; }
        }

        /// <summary>
        /// The structural mode frequency, in Hz, for the pitch/yaw axis group. Used directly as the
        /// filter frequency in <see cref="RateFilterMode.Notch"/> / <see cref="RateFilterMode.LowPass"/>
        /// mode, and as the seed for the automatic frequency estimator before it acquires. Defaults
        /// to 1.5 Hz.
        /// </summary>
        [KRPCProperty]
        public double PitchYawOscillationFrequency {
            get { return Controller.PitchYawOscillationFrequency; }
            set { Controller.PitchYawOscillationFrequency = value; }
        }

        /// <summary>
        /// The structural mode frequency, in Hz, for the roll axis. Behaves as
        /// <see cref="PitchYawOscillationFrequency"/> but for roll. Defaults to 1.5 Hz.
        /// </summary>
        [KRPCProperty]
        public double RollOscillationFrequency {
            get { return Controller.RollOscillationFrequency; }
            set { Controller.RollOscillationFrequency = value; }
        }

        /// <summary>
        /// The quality factor of the notch filter used to suppress a low-frequency structural mode.
        /// A higher value gives a narrower notch (less in-band control lag but less tolerance to the
        /// mode frequency drifting); a lower value gives a wider notch. Defaults to 2.5. This is an
        /// advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationNotchQ {
            get { return Controller.OscillationNotchQ; }
            set { Controller.OscillationNotchQ = value; }
        }

        /// <summary>
        /// Controls the bandwidth-floor mitigation: the reduction of the inner control loop
        /// bandwidth on a structurally flexible axis — the primary oscillation stabilizer. When
        /// <see cref="MitigationMode.Automatic"/> (the default) it engages on a latched axis
        /// while holding (and during a detected limit cycle). <see cref="MitigationMode.Off"/>
        /// never reduces the bandwidth; <see cref="MitigationMode.Forced"/> keeps it fully
        /// reduced at all times.
        /// </summary>
        [KRPCProperty]
        public MitigationMode OscillationBandwidthFloorMode {
            get { return Controller.BandwidthFloorMode; }
            set { Controller.BandwidthFloorMode = value; }
        }

        /// <summary>
        /// The inner control loop bandwidth, in rad/s, that an axis is reduced towards while the
        /// bandwidth-floor mitigation is engaged on it. Lowering it suppresses oscillation more
        /// strongly; raising it keeps more control authority at the cost of allowing more wobble.
        /// Defaults to 1 rad/s. This is an advanced tuning parameter.
        /// </summary>
        [KRPCProperty]
        public double OscillationBandwidthFloor {
            get { return Controller.OscillationBandwidthFloor; }
            set { Controller.OscillationBandwidthFloor = value; }
        }

        /// <summary>
        /// Controls the feedforward-cut mitigation: removal of the acceleration feedforward on a
        /// structurally flexible axis while holding, so it cannot re-excite a residual mode at
        /// the reduced bandwidth. When <see cref="MitigationMode.Automatic"/> (the default) it
        /// follows the hold gate on a latched axis. <see cref="MitigationMode.Off"/> never cuts
        /// the feedforward; <see cref="MitigationMode.Forced"/> always cuts it fully.
        /// </summary>
        [KRPCProperty]
        public MitigationMode OscillationFeedforwardMode {
            get { return Controller.FeedforwardMode; }
            set { Controller.FeedforwardMode = value; }
        }

        /// <summary>
        /// Controls the output-smoothing mitigation: a low-pass on the delivered actuator
        /// command that caps residual control chatter. When
        /// <see cref="MitigationMode.Automatic"/> (the default) it engages on a latched axis
        /// (and, lightly, while the oscillation detector is firing on an unlatched one).
        /// <see cref="MitigationMode.Off"/> never smooths; <see cref="MitigationMode.Forced"/>
        /// smooths fully at all times.
        /// </summary>
        [KRPCProperty]
        public MitigationMode OscillationOutputFilterMode {
            get { return Controller.OutputFilterMode; }
            set { Controller.OutputFilterMode = value; }
        }

        /// <summary>
        /// The current amplitude of control-output oscillation on the pitch/yaw axis group, measured as
        /// the deviation of the delivered control about its slowly-varying trim. A settled hold sits
        /// near zero; a sustained limit cycle drives it toward 1. Read-only.
        /// </summary>
        [KRPCProperty]
        public double PitchYawControlOscillation {
            get { return Controller.PitchYawControlOscillation; }
        }

        /// <summary>
        /// The current amplitude of control-output oscillation on the roll axis, measured as the
        /// deviation of the delivered control about its slowly-varying trim. Read-only. See
        /// <see cref="PitchYawControlOscillation"/>.
        /// </summary>
        [KRPCProperty]
        public double RollControlOscillation {
            get { return Controller.RollControlOscillation; }
        }

        /// <summary>
        /// A measure, between 0 and 1 for each of the pitch, roll and yaw axes, of how strongly the
        /// auto-pilot currently detects structural oscillation (wobble) on that axis. 0 means none
        /// detected; values approaching 1 mean a sustained structural oscillation. Read-only.
        /// </summary>
        [KRPCProperty]
        public Tuple3 OscillationLevel {
            get { return Controller.OscillationLevel.ToTuple (); }
        }

        /// <summary>
        /// Whether the auto-pilot has confirmed the pitch/yaw axes to be structurally flexible and
        /// latched oscillation suppression on for them. Read-only. See
        /// <see cref="PitchYawRateFilterMode"/>.
        /// </summary>
        [KRPCProperty]
        public bool PitchYawOscillationLatched {
            get { return Controller.PitchYawOscillationLatched; }
        }

        /// <summary>
        /// Whether the auto-pilot has confirmed the roll axis to be structurally flexible and latched
        /// oscillation suppression on for it. Read-only. See <see cref="RollRateFilterMode"/>.
        /// </summary>
        [KRPCProperty]
        public bool RollOscillationLatched {
            get { return Controller.RollOscillationLatched; }
        }

        /// <summary>
        /// The structural oscillation frequency, in Hz, estimated by the automatic detector for the
        /// pitch/yaw axis group, or <c>NaN</c> until the estimator acquires. The estimator runs in
        /// all modes, so this is observable even when suppression is off or forced. Read-only.
        /// </summary>
        [KRPCProperty]
        public double PitchYawOscillationDetectedFrequency {
            get { return Controller.PitchYawOscillationDetectedFrequency; }
        }

        /// <summary>
        /// The structural oscillation frequency, in Hz, estimated by the automatic detector for the
        /// roll axis, or <c>NaN</c> until the estimator acquires. Read-only. See
        /// <see cref="PitchYawOscillationDetectedFrequency"/>.
        /// </summary>
        [KRPCProperty]
        public double RollOscillationDetectedFrequency {
            get { return Controller.RollOscillationDetectedFrequency; }
        }

        /// <summary>
        /// When <c>true</c>, records one row of diagnostic data per physics tick to an
        /// in-memory buffer (see <see cref="DiagnosticLog"/>), and echoes each row to
        /// Player.log prefixed with <c>[KRPC.AP]</c>. The data is CSV: the first row is a
        /// header naming every column, and each subsequent row records the auto-pilot's full
        /// control-loop state for one tick (setpoints, errors, measured rates, gains,
        /// velocity-profile and feedforward internals, control outputs, and the oscillation
        /// detector/gate/mitigation state). The buffer is capped at 3000 data rows (one minute
        /// at the 50 Hz physics rate); when full, this property switches itself back to
        /// <c>false</c> and the buffer holds the minute following the enable. Setting to
        /// <c>true</c> clears the buffer. Defaults to <c>false</c>.
        /// </summary>
        [KRPCProperty]
        public bool DiagnosticLogging {
            get { return Controller.DiagnosticLogging; }
            set { Controller.DiagnosticLogging = value; }
        }

        /// <summary>
        /// The diagnostic log collected since <see cref="DiagnosticLogging"/> was last set to
        /// <c>true</c>: CSV text whose first line is the column header and each subsequent
        /// line records one physics tick. Vector-valued channels use one column per component
        /// (suffixed <c>.p/.r/.y</c> for pitch, roll, yaw); pitch-yaw-group/roll channel pairs
        /// are suffixed <c>.py/.roll</c>. Returns an empty string if diagnostic logging has
        /// not been enabled or no ticks have occurred.
        /// </summary>
        [KRPCProperty]
        public string DiagnosticLog {
            get { return Controller.GetDiagnosticLog (); }
        }

        /// <summary>
        /// Measured angular velocity (raw, roll-invariant frame, rad/s) for the in-game info window.
        /// </summary>
        internal Tuple3 MeasuredAngularVelocity {
            get { return Controller.MeasuredAngularVelocity.ToTuple (); }
        }

        /// <summary>
        /// Target angular velocity the inner loop is tracking (roll-invariant frame, rad/s) for the
        /// in-game info window.
        /// </summary>
        internal Tuple3 TargetAngularVelocity {
            get { return Controller.TargetAngularVelocity.ToTuple (); }
        }

        /// <summary>
        /// Per-axis hold-gated mitigation weight in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 MitigationLevel {
            get { return Controller.MitigationLevel.ToTuple (); }
        }

        /// <summary>
        /// The control-output oscillation envelope above which the hold mitigation engages
        /// automatically, for the in-game info window's envelope-readout colouring.
        /// </summary>
        internal double OscillationControlThreshold {
            get { return Controller.OscillationControlThreshold; }
        }

        /// <summary>
        /// Whether the engaged auto-pilot is being held inert on the launch clamps (PRELAUNCH),
        /// for the in-game info window's ENGAGED/HELD lamp.
        /// </summary>
        internal bool Held {
            get { return InternalVessel.situation == global::Vessel.Situations.PRELAUNCH; }
        }

        /// <summary>
        /// The pitch/yaw hold factor in [0,1] (keyed on the pointing error) for the in-game
        /// info window (1 = holding, 0 = slewing).
        /// </summary>
        internal double PitchYawHoldFactor {
            get { return Controller.PitchYawHoldFactor; }
        }

        /// <summary>
        /// The roll hold factor in [0,1] (keyed on the larger of the pointing and roll errors)
        /// for the in-game info window.
        /// </summary>
        internal double RollHoldFactor {
            get { return Controller.RollHoldFactor; }
        }

        /// <summary>
        /// Per-axis latch (suppression) ramp in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 SuppressionRamp {
            get { return Controller.SuppressionRamp.ToTuple (); }
        }

        /// <summary>
        /// Per-axis oscillation-control back-off in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 OscillationBackoff {
            get { return Controller.OscillationBackoff.ToTuple (); }
        }

        /// <summary>
        /// Per-axis applied feedforward-cut fraction in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 FeedforwardCut {
            get { return Controller.FeedforwardCut.ToTuple (); }
        }

        /// <summary>
        /// Per-axis output-filter blend weight in [0,1] for the in-game info window.
        /// </summary>
        internal Tuple3 OutputFilterWeight {
            get { return Controller.OutputFilterWeight.ToTuple (); }
        }

        /// <summary>
        /// Active suppression tool on an axis (0 none, 1 notch, 2 low-pass) for the in-game info
        /// window.
        /// </summary>
        internal int ActiveSuppressionTool (int axis)
        {
            return Controller.ActiveSuppressionTool (axis);
        }

        /// <summary>
        /// The oscillation level at which an axis latches as flexible, for the in-game info window.
        /// </summary>
        internal double OscillationLatchThreshold {
            get { return Controller.OscillationLatchThreshold; }
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

    }
}
