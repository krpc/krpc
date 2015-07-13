using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPCSpaceCenter.Services
{
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

        [KRPCProperty]
        public bool SAS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        [KRPCProperty]
        public bool RCS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

        [KRPCProperty]
        public bool Gear {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Gear)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Gear, value); }
        }

        [KRPCProperty]
        public bool Lights {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Light)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Light, value); }
        }

        [KRPCProperty]
        public bool Brakes {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Brakes)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, value); }
        }

        [KRPCProperty]
        public bool Abort {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Abort)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Abort, value); }
        }

        [KRPCProperty]
        public float Throttle {
            get { return vessel.isActiveVessel ? FlightInputHandler.state.mainThrottle : vessel.ctrlState.mainThrottle; }
            set {
                if (vessel.isActiveVessel)
                    FlightInputHandler.state.mainThrottle = value;
                else
                    vessel.ctrlState.mainThrottle = value;
            }
        }

        [KRPCProperty]
        public float Pitch {
            get { return PilotAddon.Get (vessel).Pitch; }
            set { PilotAddon.Get (vessel).Pitch = value; }
        }

        [KRPCProperty]
        public float Yaw {
            get { return PilotAddon.Get (vessel).Yaw; }
            set { PilotAddon.Get (vessel).Yaw = value; }
        }

        [KRPCProperty]
        public float Roll {
            get { return PilotAddon.Get (vessel).Roll; }
            set { PilotAddon.Get (vessel).Roll = value; }
        }

        [KRPCProperty]
        public float Forward {
            get { return PilotAddon.Get (vessel).Forward; }
            set { PilotAddon.Get (vessel).Forward = value; }
        }

        [KRPCProperty]
        public float Up {
            get { return PilotAddon.Get (vessel).Up; }
            set { PilotAddon.Get (vessel).Up = value; }
        }

        [KRPCProperty]
        public float Right {
            get { return PilotAddon.Get (vessel).Right; }
            set { PilotAddon.Get (vessel).Right = value; }
        }

        [KRPCProperty]
        public float WheelThrottle {
            get { return PilotAddon.Get (vessel).WheelThrottle; }
            set { PilotAddon.Get (vessel).WheelThrottle = value; }
        }

        [KRPCProperty]
        public float WheelSteering {
            get { return PilotAddon.Get (vessel).WheelSteer; }
            set { PilotAddon.Get (vessel).WheelSteer = value; }
        }

        [KRPCProperty]
        public int CurrentStage {
            get { return vessel.currentStage; }
        }

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

        [KRPCMethod]
        public bool GetActionGroup (uint group)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (Utils.GetActionGroup (group))];
        }

        [KRPCMethod]
        public void SetActionGroup (uint group, bool state)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            vessel.ActionGroups.SetGroup (Utils.GetActionGroup (group), state);
        }

        [KRPCMethod]
        public void ToggleActionGroup (uint group)
        {
            if (group > 9)
                throw new ArgumentException ("Action group must be between 0 and 9 inclusive");
            vessel.ActionGroups.ToggleGroup (Utils.GetActionGroup (group));
        }

        [KRPCMethod]
        public Node AddNode (double UT, float prograde = 0, float normal = 0, float radial = 0)
        {
            if (!vessel.isActiveVessel)
                throw new InvalidOperationException ("Cannot add maneuver node; vessel is not the active vessel");
            return new Node (vessel, UT, prograde, normal, radial);
        }

        [KRPCProperty]
        public IList<Node> Nodes {
            get {
                if (!vessel.isActiveVessel)
                    throw new InvalidOperationException ("Cannot get maneuver nodes; vessel is not the active vessel");
                return vessel.patchedConicSolver.maneuverNodes.Select (x => new Node (x)).OrderBy (x => x.UT).ToList ();
            }
        }

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
