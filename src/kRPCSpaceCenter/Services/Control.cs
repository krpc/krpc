using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

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
        // FIXME: what if vessel is not the active vessel?
        [KRPCProperty]
        public float Throttle {
            get { return FlightInputHandler.state.mainThrottle; }
            set { FlightInputHandler.state.mainThrottle = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Forward {
            get { return PilotAddon.X; }
            set { PilotAddon.X = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Up {
            get { return PilotAddon.Y; }
            set { PilotAddon.Y = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Sideways {
            get { return PilotAddon.Z; }
            set { PilotAddon.Z = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Pitch {
            get { return PilotAddon.Pitch; }
            set { PilotAddon.Pitch = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Roll {
            get { return PilotAddon.Roll; }
            set { PilotAddon.Roll = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float Yaw {
            get { return PilotAddon.Yaw; }
            set { PilotAddon.Yaw = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float WheelThrottle {
            get { return PilotAddon.WheelThrottle; }
            set { PilotAddon.WheelThrottle = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public float WheelSteering {
            get { return PilotAddon.WheelSteer; }
            set { PilotAddon.WheelSteer = value.Clamp (-1f, 1f); }
        }

        [KRPCProperty]
        public int CurrentStage {
            get { return Staging.CurrentStage; }
        }

        [KRPCMethod]
        public IList<Vessel> ActivateNextStage ()
        {
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
        public Node AddNode (double UT, double prograde = 0, double normal = 0, double radial = 0)
        {
            return new Node (vessel, UT, prograde, normal, radial);
        }

        [KRPCProperty]
        public IList<Node> Nodes {
            get {
                return vessel.patchedConicSolver.maneuverNodes.Select (x => new Node (x)).OrderBy (x => x.UT).ToList ();
            }
        }

        [KRPCMethod]
        public void RemoveNodes ()
        {
            var nodes = vessel.patchedConicSolver.maneuverNodes.ToArray ();
            foreach (var node in nodes)
                node.RemoveSelf ();
            // TODO: delete the Node objects
        }
    }
}
