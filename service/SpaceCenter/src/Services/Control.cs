using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Used to manipulate the controls of a vessel. This includes adjusting the
    /// throttle, enabling/disabling systems such as SAS and RCS, or altering the
    /// direction in which the vessel is pointing.
    /// </summary>
    /// <remarks>
    /// Control inputs (such as pitch, yaw and roll) are zeroed when all clients
    /// that have set one or more of these inputs are no longer connected.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Control : Equatable<Control>
    {
        readonly global::Vessel vessel;

        internal Control (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        public override bool Equals (Control obj)
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
        /// <remarks>Equivalent to <see cref="AutoPilot.SAS"/></remarks>
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
        /// <remarks>Equivalent to <see cref="AutoPilot.SASMode"/></remarks>
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
            get { return GetSpeedMode (); }
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

        internal static SpeedMode GetSpeedMode ()
        {
            return FlightUIController.speedDisplayMode.ToSpeedMode ();
        }

        /// <summary>
        /// The state of RCS.
        /// </summary>
        [KRPCProperty]
        public bool RCS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

        /// <summary>
        /// The state of the landing gear/legs.
        /// </summary>
        [KRPCProperty]
        public bool Gear {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Gear)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Gear, value); }
        }

        /// <summary>
        /// The state of the lights.
        /// </summary>
        [KRPCProperty]
        public bool Lights {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Light)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Light, value); }
        }

        /// <summary>
        /// The state of the wheel brakes.
        /// </summary>
        [KRPCProperty]
        public bool Brakes {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Brakes)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, value); }
        }

        /// <summary>
        /// The state of the abort action group.
        /// </summary>
        [KRPCProperty]
        public bool Abort {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Abort)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Abort, value); }
        }

        /// <summary>
        /// The state of the throttle. A value between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return PilotAddon.Get (vessel).Throttle; }
            set { PilotAddon.Set (vessel).Throttle = value; }
        }

        /// <summary>
        /// The state of the pitch control.
        /// A value between -1 and 1.
        /// Equivalent to the w and s keys.
        /// </summary>
        [KRPCProperty]
        public float Pitch {
            get { return PilotAddon.Get (vessel).Pitch; }
            set { PilotAddon.Set (vessel).Pitch = value; }
        }

        /// <summary>
        /// The state of the yaw control.
        /// A value between -1 and 1.
        /// Equivalent to the a and d keys.
        /// </summary>
        [KRPCProperty]
        public float Yaw {
            get { return PilotAddon.Get (vessel).Yaw; }
            set { PilotAddon.Set (vessel).Yaw = value; }
        }

        /// <summary>
        /// The state of the roll control.
        /// A value between -1 and 1.
        /// Equivalent to the q and e keys.
        /// </summary>
        [KRPCProperty]
        public float Roll {
            get { return PilotAddon.Get (vessel).Roll; }
            set { PilotAddon.Set (vessel).Roll = value; }
        }

        /// <summary>
        /// The state of the forward translational control.
        /// A value between -1 and 1.
        /// Equivalent to the h and n keys.
        /// </summary>
        [KRPCProperty]
        public float Forward {
            get { return PilotAddon.Get (vessel).Forward; }
            set { PilotAddon.Set (vessel).Forward = value; }
        }

        /// <summary>
        /// The state of the up translational control.
        /// A value between -1 and 1.
        /// Equivalent to the i and k keys.
        /// </summary>
        [KRPCProperty]
        public float Up {
            get { return PilotAddon.Get (vessel).Up; }
            set { PilotAddon.Set (vessel).Up = value; }
        }

        /// <summary>
        /// The state of the right translational control.
        /// A value between -1 and 1.
        /// Equivalent to the j and l keys.
        /// </summary>
        [KRPCProperty]
        public float Right {
            get { return PilotAddon.Get (vessel).Right; }
            set { PilotAddon.Set (vessel).Right = value; }
        }

        /// <summary>
        /// The state of the wheel throttle.
        /// A value between -1 and 1.
        /// A value of 1 rotates the wheels forwards, a value of -1 rotates
        /// the wheels backwards.
        /// </summary>
        [KRPCProperty]
        public float WheelThrottle {
            get { return PilotAddon.Get (vessel).WheelThrottle; }
            set { PilotAddon.Set (vessel).WheelThrottle = value; }
        }

        /// <summary>
        /// The state of the wheel steering.
        /// A value between -1 and 1.
        /// A value of 1 steers to the left, and a value of -1 steers to the right.
        /// </summary>
        [KRPCProperty]
        public float WheelSteering {
            get { return PilotAddon.Get (vessel).WheelSteer; }
            set { PilotAddon.Set (vessel).WheelSteer = value; }
        }

        /// <summary>
        /// The current stage of the vessel. Corresponds to the stage number in
        /// the in-game UI.
        /// </summary>
        [KRPCProperty]
        public int CurrentStage {
            get { return vessel.currentStage; }
        }

        /// <summary>
        /// Activates the next stage. Equivalent to pressing the space bar in-game.
        /// </summary>
        /// <returns>A list of vessel objects that are jettisoned from the active vessel.</returns>
        [KRPCMethod]
        public IList<Vessel> ActivateNextStage ()
        {
            if (!vessel.isActiveVessel)
                throw new InvalidOperationException ("Cannot activate stage; vessel is not the active vessel");
            if (!Staging.separate_ready)
                throw new YieldException (new ParameterizedContinuation<IList<Vessel>> (ActivateNextStage));
            var preVessels = FlightGlobals.Vessels.ToArray ();
            Staging.ActivateNextStage ();
            return PostActivateStage (preVessels);
        }

        IList<Vessel> PostActivateStage (global::Vessel[] preVessels)
        {
            if (!Staging.separate_ready)
                throw new YieldException (new ParameterizedContinuation<IList<Vessel>, global::Vessel[]> (PostActivateStage, preVessels));
            var postVessels = FlightGlobals.Vessels;
            return postVessels.Except (preVessels).Select (vessel => new Vessel (vessel)).ToList ();
        }

        /// <summary>
        /// Returns <c>true</c> if the given action group is enabled.
        /// </summary>
        /// <param name="group">A number between 0 and 9 inclusive.</param>
        [KRPCMethod]
        public bool GetActionGroup (uint group)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (ActionGroupExtensions.GetActionGroup (group))];
        }

        /// <summary>
        /// Sets the state of the given action group (a value between 0 and 9
        /// inclusive).
        /// </summary>
        /// <param name="group">A number between 0 and 9 inclusive.</param>
        /// <param name="state"></param>
        [KRPCMethod]
        public void SetActionGroup (uint group, bool state)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            vessel.ActionGroups.SetGroup (ActionGroupExtensions.GetActionGroup (group), state);
        }

        /// <summary>
        /// Toggles the state of the given action group.
        /// </summary>
        /// <param name="group">A number between 0 and 9 inclusive.</param>
        [KRPCMethod]
        public void ToggleActionGroup (uint group)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            vessel.ActionGroups.ToggleGroup (ActionGroupExtensions.GetActionGroup (group));
        }

        /// <summary>
        /// Creates a maneuver node at the given universal time, and returns a
        /// <see cref="Node"/> object that can be used to modify it.
        /// Optionally sets the magnitude of the delta-v for the maneuver node
        /// in the prograde, normal and radial directions.
        /// </summary>
        /// <param name="UT">Universal time of the maneuver node.</param>
        /// <param name="prograde">Delta-v in the prograde direction.</param>
        /// <param name="normal">Delta-v in the normal direction.</param>
        /// <param name="radial">Delta-v in the radial direction.</param>
        [KRPCMethod]
        public Node AddNode (double UT, float prograde = 0, float normal = 0, float radial = 0)
        {
            if (!vessel.isActiveVessel)
                throw new InvalidOperationException ("Cannot add maneuver node; vessel is not the active vessel");
            return new Node (vessel, UT, prograde, normal, radial);
        }

        /// <summary>
        /// Returns a list of all existing maneuver nodes, ordered by time from first to last.
        /// </summary>
        [KRPCProperty]
        public IList<Node> Nodes {
            get {
                if (!vessel.isActiveVessel)
                    throw new InvalidOperationException ("Cannot get maneuver nodes; vessel is not the active vessel");
                return vessel.patchedConicSolver.maneuverNodes.Select (x => new Node (x)).OrderBy (x => x.UT).ToList ();
            }
        }

        /// <summary>
        /// Remove all maneuver nodes.
        /// </summary>
        [KRPCMethod]
        public void RemoveNodes ()
        {
            if (!vessel.isActiveVessel)
                throw new InvalidOperationException ("Cannot remove maneuver ndoes; vessel is not the active vessel");
            var nodes = vessel.patchedConicSolver.maneuverNodes.ToArray ();
            foreach (var node in nodes)
                node.RemoveSelf ();
            // TODO: delete the Node objects
        }
    }
}
