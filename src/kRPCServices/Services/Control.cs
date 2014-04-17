using System;
using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Control
    {
        global::Vessel vessel;

        internal Control (global::Vessel vessel)
        {
            this.vessel = vessel;
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

        // FIXME: what if vessel is not the active vessel?
        [KRPCProperty]
        public float Throttle {
            get { return FlightInputHandler.state.mainThrottle; }
            set { FlightInputHandler.state.mainThrottle = value; }
        }

        [KRPCProperty]
        public float Forward {
            get { return PilotAddon.X; }
            set { PilotAddon.X = value; }
        }

        [KRPCProperty]
        public float Up {
            get { return PilotAddon.Y; }
            set { PilotAddon.Y = value; }
        }

        [KRPCProperty]
        public float Sideways {
            get { return PilotAddon.Z; }
            set { PilotAddon.Z = value; }
        }

        [KRPCProperty]
        public float Pitch {
            get { return PilotAddon.Pitch; }
            set { PilotAddon.Pitch = value; }
        }

        [KRPCProperty]
        public float Roll {
            get { return PilotAddon.Roll; }
            set { PilotAddon.Roll = value; }
        }

        [KRPCProperty]
        public float Yaw {
            get { return PilotAddon.Yaw; }
            set { PilotAddon.Yaw = value; }
        }

        [KRPCMethod]
        public void ActivateNextStage ()
        {
            Staging.ActivateNextStage ();
        }

        [KRPCMethod]
        public bool GetActionGroup (uint group)
        {
            return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (Utils.GetActionGroup ((int) group))];
        }

        [KRPCMethod]
        public void SetActionGroup (uint group, bool state)
        {
            vessel.ActionGroups.SetGroup (Utils.GetActionGroup ((int) group), state);
        }

        [KRPCMethod]
        public void ToggleActionGroup (uint group)
        {
            throw new NotImplementedException ();
        }

        [KRPCMethod]
        public Node AddNode (ulong UT, double prograde = 0, double normal = 0, double radial = 0)
        {
            return new Node (vessel, UT, prograde, normal, radial);
        }

        [KRPCMethod]
        public void RemoveNodes ()
        {
            throw new NotImplementedException ();
        }
    }
}
