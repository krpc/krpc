using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.ExternalAPI;
using KRPC.Utils;
using KSP.UI.Screens;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to manipulate the controls of a vessel. This includes adjusting the
    /// throttle, enabling/disabling systems such as SAS and RCS, or altering the
    /// direction in which the vessel is pointing.
    /// Obtained by calling <see cref="Vessel.Control"/>.
    /// </summary>
    /// <remarks>
    /// Control inputs (such as pitch, yaw and roll) are zeroed when all clients
    /// that have set one or more of these inputs are no longer connected.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public class Control : Equatable<Control>
    {
        readonly Guid vesselId;
        readonly Parts.Parts parts;

        internal Control (global::Vessel vessel)
        {
            vesselId = vessel.id;
            parts = new Vessel (vessel).Parts;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Control other)
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
        /// The control state of the vessel.
        /// </summary>
        [KRPCProperty]
        public ControlState State {
            get { return InternalVessel.Connection.ControlState.ToControlState (); }
        }

        /// <summary>
        /// The source of the vessels control, for example by a kerbal or a probe core.
        /// </summary>
        [KRPCProperty]
        public ControlSource Source {
            get { return InternalVessel.Connection.ControlState.ToControlSource (); }
        }

        /// <summary>
        /// The state of SAS.
        /// </summary>
        /// <remarks>Equivalent to <see cref="AutoPilot.SAS"/></remarks>
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
        /// <remarks>Equivalent to <see cref="AutoPilot.SASMode"/></remarks>
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
            var mode = value.FromSASMode ();
            if (!vessel.Autopilot.CanSetMode (mode))
                throw new InvalidOperationException ("Cannot set SAS mode of vessel");
            vessel.Autopilot.SetMode (mode);
            // Update the UI buttons
            var modeIndex = (int)vessel.Autopilot.Mode;
            var modeButtons = UnityEngine.Object.FindObjectOfType<VesselAutopilotUI> ().modeButtons;
            modeButtons [modeIndex].SetState (true);
        }

        /// <summary>
        /// The current <see cref="SpeedMode"/> of the navball.
        /// This is the mode displayed next to the speed at the top of the navball.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public SpeedMode SpeedMode {
            get { return GlobalSpeedMode; }
            set { FlightGlobals.SetSpeedMode (value.FromSpeedMode ()); }
        }

        [KRPCProperty]
        internal static SpeedMode GlobalSpeedMode {
            get { return FlightGlobals.speedDisplayMode.ToSpeedMode (); }
        }

        /// <summary>
        /// The state of RCS.
        /// </summary>
        [KRPCProperty]
        public bool RCS {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

        /// <summary>
        /// Returns whether all reactive wheels on the vessel are active,
        /// and sets the active state of all reaction wheels.
        /// See <see cref="Parts.ReactionWheel.Active"/>.
        /// </summary>
        [KRPCProperty]
        public bool ReactionWheels {
            get { return parts.ReactionWheels.All (part => part.Active); }
            set {
                foreach (var part in parts.ReactionWheels)
                    part.Active = value;
            }
        }

        /// <summary>
        /// The state of the landing gear/legs.
        /// </summary>
        [KRPCProperty]
        public bool Gear {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Gear)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.Gear, value); }
        }

        /// <summary>
        /// Returns whether all landing legs on the vessel are deployed,
        /// and sets the deployment state of all landing legs.
        /// Does not include wheels (for example landing gear).
        /// See <see cref="Parts.Leg.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool Legs {
            get { return parts.Legs.All (part => part.Deployed); }
            set {
                foreach (var part in parts.Legs)
                    if (part.Deployable)
                        part.Deployed = value;
            }
        }

        /// <summary>
        /// Returns whether all wheels on the vessel are deployed,
        /// and sets the deployment state of all wheels.
        /// Does not include landing legs.
        /// See <see cref="Parts.Wheel.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool Wheels {
            get { return parts.Wheels.All (part => part.Deployed); }
            set {
                foreach (var part in parts.Wheels)
                    if (part.Deployable)
                        part.Deployed = value;
            }
        }

        /// <summary>
        /// The state of the lights.
        /// </summary>
        [KRPCProperty]
        public bool Lights {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Light)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.Light, value); }
        }

        /// <summary>
        /// The state of the wheel brakes.
        /// </summary>
        [KRPCProperty]
        public bool Brakes {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Brakes)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, value); }
        }

        /// <summary>
        /// Returns whether all antennas on the vessel are deployed,
        /// and sets the deployment state of all antennas.
        /// See <see cref="Parts.Antenna.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool Antennas {
            get { return parts.Antennas.All (part => part.Deployed); }
            set {
                foreach (var part in parts.Antennas)
                    if (part.Deployable)
                            part.Deployed = value;
            }
        }

        /// <summary>
        /// Returns whether any of the cargo bays on the vessel are open,
        /// and sets the open state of all cargo bays.
        /// See <see cref="Parts.CargoBay.Open"/>.
        /// </summary>
        [KRPCProperty]
        public bool CargoBays {
            get { return parts.CargoBays.Any (part => part.Open); }
            set {
                foreach (var part in parts.CargoBays)
                    part.Open = value;
            }
        }

        /// <summary>
        /// Returns whether all of the air intakes on the vessel are open,
        /// and sets the open state of all air intakes.
        /// See <see cref="Parts.Intake.Open"/>.
        /// </summary>
        [KRPCProperty]
        public bool Intakes {
            get { return parts.Intakes.All (part => part.Open); }
            set {
                foreach (var part in parts.Intakes)
                    part.Open = value;
            }
        }

        /// <summary>
        /// Returns whether all parachutes on the vessel are deployed,
        /// and sets the deployment state of all parachutes.
        /// Cannot be set to <c>false</c>.
        /// See <see cref="Parts.Parachute.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool Parachutes {
            get { return parts.Parachutes.All (part => part.Deployed); }
            set {
                if (!value)
                    throw new ArgumentException ("Cannot 'un-deploy' parachutes", nameof (Parachutes));
                foreach (var part in parts.Parachutes)
                    part.Deploy ();
            }
        }

        /// <summary>
        /// Returns whether all radiators on the vessel are deployed,
        /// and sets the deployment state of all radiators.
        /// See <see cref="Parts.Radiator.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool Radiators {
            get { return parts.Radiators.All (part => part.Deployed); }
            set {
                foreach (var part in parts.Radiators)
                    if (part.Deployable)
                            part.Deployed = value;
            }
        }

        /// <summary>
        /// Returns whether all of the resource harvesters on the vessel are deployed,
        /// and sets the deployment state of all resource harvesters.
        /// See <see cref="Parts.ResourceHarvester.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool ResourceHarvesters {
            get { return parts.ResourceHarvesters.All (part => part.Deployed); }
            set {
                foreach (var part in parts.ResourceHarvesters)
                    part.Deployed = value;
            }
        }

        /// <summary>
        /// Returns whether any of the resource harvesters on the vessel are active,
        /// and sets the active state of all resource harvesters.
        /// See <see cref="Parts.ResourceHarvester.Active"/>.
        /// </summary>
        [KRPCProperty]
        public bool ResourceHarvestersActive {
            get { return parts.ResourceHarvesters.Any (part => part.Active); }
            set {
                foreach (var part in parts.ResourceHarvesters)
                    part.Active = value;
            }
        }

        /// <summary>
        /// Returns whether all solar panels on the vessel are deployed,
        /// and sets the deployment state of all solar panels.
        /// See <see cref="Parts.SolarPanel.Deployed"/>.
        /// </summary>
        [KRPCProperty]
        public bool SolarPanels {
            get { return parts.SolarPanels.All (part => part.Deployed); }
            set {
                foreach (var part in parts.SolarPanels)
                    if (part.Deployable)
                        part.Deployed = value;
            }
        }

        /// <summary>
        /// The state of the abort action group.
        /// </summary>
        [KRPCProperty]
        public bool Abort {
            get { return InternalVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Abort)]; }
            set { InternalVessel.ActionGroups.SetGroup (KSPActionGroup.Abort, value); }
        }

        /// <summary>
        /// The state of the throttle. A value between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return PilotAddon.Get (InternalVessel).Throttle; }
            set { PilotAddon.Set (InternalVessel).Throttle = value; }
        }

        /// <summary>
        /// The state of the pitch control.
        /// A value between -1 and 1.
        /// Equivalent to the w and s keys.
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get { return PilotAddon.Get (InternalVessel).Pitch; }
            set { PilotAddon.Set (InternalVessel).Pitch = value; }
        }

        /// <summary>
        /// The state of the yaw control.
        /// A value between -1 and 1.
        /// Equivalent to the a and d keys.
        /// </summary>
        [KRPCProperty]
        public float Yaw {
            get { return PilotAddon.Get (InternalVessel).Yaw; }
            set { PilotAddon.Set (InternalVessel).Yaw = value; }
        }

        /// <summary>
        /// The state of the roll control.
        /// A value between -1 and 1.
        /// Equivalent to the q and e keys.
        /// </summary>
        [KRPCProperty]
        public float Roll {
            get { return PilotAddon.Get (InternalVessel).Roll; }
            set { PilotAddon.Set (InternalVessel).Roll = value; }
        }

        /// <summary>
        /// The state of the forward translational control.
        /// A value between -1 and 1.
        /// Equivalent to the h and n keys.
        /// </summary>
        [KRPCProperty]
        public float Forward {
            get { return PilotAddon.Get (InternalVessel).Forward; }
            set { PilotAddon.Set (InternalVessel).Forward = value; }
        }

        /// <summary>
        /// The state of the up translational control.
        /// A value between -1 and 1.
        /// Equivalent to the i and k keys.
        /// </summary>
        [KRPCProperty]
        public float Up {
            get { return PilotAddon.Get (InternalVessel).Up; }
            set { PilotAddon.Set (InternalVessel).Up = value; }
        }

        /// <summary>
        /// The state of the right translational control.
        /// A value between -1 and 1.
        /// Equivalent to the j and l keys.
        /// </summary>
        [KRPCProperty]
        public float Right {
            get { return PilotAddon.Get (InternalVessel).Right; }
            set { PilotAddon.Set (InternalVessel).Right = value; }
        }

        /// <summary>
        /// The state of the wheel throttle.
        /// A value between -1 and 1.
        /// A value of 1 rotates the wheels forwards, a value of -1 rotates
        /// the wheels backwards.
        /// </summary>
        [KRPCProperty]
        public float WheelThrottle {
            get { return PilotAddon.Get (InternalVessel).WheelThrottle; }
            set { PilotAddon.Set (InternalVessel).WheelThrottle = value; }
        }

        /// <summary>
        /// The state of the wheel steering.
        /// A value between -1 and 1.
        /// A value of 1 steers to the left, and a value of -1 steers to the right.
        /// </summary>
        [KRPCProperty]
        public float WheelSteering {
            get { return PilotAddon.Get (InternalVessel).WheelSteer; }
            set { PilotAddon.Set (InternalVessel).WheelSteer = value; }
        }

        /// <summary>
        /// The current stage of the vessel. Corresponds to the stage number in
        /// the in-game UI.
        /// </summary>
        [KRPCProperty]
        public int CurrentStage {
            get { return InternalVessel.currentStage; }
        }

        /// <summary>
        /// Activates the next stage. Equivalent to pressing the space bar in-game.
        /// </summary>
        /// <returns>A list of vessel objects that are jettisoned from the active vessel.</returns>
        /// <remarks>
        /// When called, the active vessel may change. It is therefore possible that,
        /// after calling this function, the object(s) returned by previous call(s) to
        /// <see cref="SpaceCenter.ActiveVessel"/> no longer refer to the active vessel.
        /// </remarks>
        [KRPCMethod]
        public IList<Vessel> ActivateNextStage ()
        {
            CheckActiveVessel ();
            if (!StageManager.CanSeparate)
                throw new YieldException (new ParameterizedContinuation<IList<Vessel>> (ActivateNextStage));
            var preVessels = FlightGlobals.Vessels.ToArray ();
            StageManager.ActivateNextStage ();
            return PostActivateStage (preVessels);
        }

        IList<Vessel> PostActivateStage (global::Vessel[] preVessels)
        {
            if (!StageManager.CanSeparate)
                throw new YieldException (new ParameterizedContinuation<IList<Vessel>, global::Vessel[]> (PostActivateStage, preVessels));
            var postVessels = FlightGlobals.Vessels;
            return postVessels.Except (preVessels).Select (vessel => new Vessel (vessel)).ToList ();
        }

        /// <summary>
        /// Returns <c>true</c> if the given action group is enabled.
        /// </summary>
        /// <param name="group">
        /// A number between 0 and 9 inclusive,
        /// or between 0 and 250 inclusive when the <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/67235-12oct3116-action-groups-extended-250-action-groups-in-flight-editing-now-kosremotetech">Extended Action Groups mod</a> is installed.
        /// </param>
        [KRPCMethod]
        public bool GetActionGroup (uint group)
        {
            var vessel = InternalVessel;
            if (AGX.IsAvailable) {
                if (group > 250)
                    throw new ArgumentException ("Action group must be between 0 and 250 inclusive");
                return AGX.AGX2VslGroupState (vessel.rootPart.flightID, (int)group);
            } else {
                if (group > 9)
                    throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
                return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (ActionGroupExtensions.GetActionGroup (group))];
            }
        }

        /// <summary>
        /// Sets the state of the given action group.
        /// </summary>
        /// <param name="group">
        /// A number between 0 and 9 inclusive,
        /// or between 0 and 250 inclusive when the <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/67235-12oct3116-action-groups-extended-250-action-groups-in-flight-editing-now-kosremotetech">Extended Action Groups mod</a> is installed.
        /// </param>
        /// <param name="state"></param>
        [KRPCMethod]
        public void SetActionGroup (uint group, bool state)
        {
            var vessel = InternalVessel;
            if (AGX.IsAvailable) {
                if (group > 250)
                    throw new ArgumentException ("Action group must be between 0 and 250 inclusive");
                AGX.AGX2VslActivateGroup (vessel.rootPart.flightID, (int)group, state);
            } else {
                if (group > 9)
                    throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
                vessel.ActionGroups.SetGroup (ActionGroupExtensions.GetActionGroup (group), state);
            }
        }

        /// <summary>
        /// Toggles the state of the given action group.
        /// </summary>
        /// <param name="group">
        /// A number between 0 and 9 inclusive,
        /// or between 0 and 250 inclusive when the <a href="http://forum.kerbalspaceprogram.com/index.php?/topic/67235-12oct3116-action-groups-extended-250-action-groups-in-flight-editing-now-kosremotetech">Extended Action Groups mod</a> is installed.
        /// </param>
        [KRPCMethod]
        public void ToggleActionGroup (uint group)
        {
            var vessel = InternalVessel;
            if (AGX.IsAvailable) {
                if (group > 250)
                    throw new ArgumentException ("Action group must be between 0 and 250 inclusive");
                AGX.AGX2VslToggleGroup (vessel.rootPart.flightID, (int)group);
            } else {
                if (group > 9)
                    throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
                vessel.ActionGroups.ToggleGroup (ActionGroupExtensions.GetActionGroup (group));
            }
        }

        /// <summary>
        /// Creates a maneuver node at the given universal time, and returns a
        /// <see cref="Node"/> object that can be used to modify it.
        /// Optionally sets the magnitude of the delta-v for the maneuver node
        /// in the prograde, normal and radial directions.
        /// </summary>
        /// <param name="ut">Universal time of the maneuver node.</param>
        /// <param name="prograde">Delta-v in the prograde direction.</param>
        /// <param name="normal">Delta-v in the normal direction.</param>
        /// <param name="radial">Delta-v in the radial direction.</param>
        [KRPCMethod]
        public Node AddNode (double ut, float prograde = 0, float normal = 0, float radial = 0)
        {
            CheckManeuverNodes ();
            return new Node (InternalVessel, ut, prograde, normal, radial);
        }

        /// <summary>
        /// Returns a list of all existing maneuver nodes, ordered by time from first to last.
        /// </summary>
        [KRPCProperty]
        public IList<Node> Nodes {
            get {
                CheckManeuverNodes ();
                return FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Select (x => new Node (FlightGlobals.ActiveVessel, x)).OrderBy (x => x.UT).ToList ();
            }
        }

        /// <summary>
        /// Remove all maneuver nodes.
        /// </summary>
        [KRPCMethod]
        public void RemoveNodes ()
        {
            CheckManeuverNodes ();
            foreach (var node in InternalVessel.patchedConicSolver.maneuverNodes.ToArray ())
                node.RemoveSelf ();
            // TODO: delete the Node objects
        }

        void CheckActiveVessel ()
        {
            if (vesselId != FlightGlobals.ActiveVessel.id)
                throw new InvalidOperationException ("Not the active vessel");
        }

        void CheckManeuverNodes ()
        {
            CheckActiveVessel ();
            if (FlightGlobals.ActiveVessel.patchedConicSolver == null)
                throw new InvalidOperationException ("Maneuver node editing is not available. Either the vessel is in a situation where maneuver nodes cannot be used, or the tracking station has not been upgraded to support them.");
        }
    }
}
