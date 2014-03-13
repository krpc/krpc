using KRPC.Service.Attributes;
using KSP;

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

        /// <summary>
        /// SAS enabled/disabled for the active vessel
        /// </summary>
        [KRPCProperty]
        public bool SAS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        /// <summary>
        /// RCS enabled/disabled for the active vessel
        /// </summary>
        [KRPCProperty]
        public bool RCS {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

        /// <summary>
        /// Landing gear/legs deployed/retracted for the active vessel
        /// </summary>
        [KRPCProperty]
        public bool Gear {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Gear)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Gear, value); }
        }

        /// <summary>
        /// Lights on/off for the active vessel
        /// </summary>
        [KRPCProperty]
        public bool Lights {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Light)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Light, value); }
        }

        /// <summary>
        /// Brakes on/off for the active vessel
        /// </summary>
        [KRPCProperty]
        public bool Brakes {
            get { return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Brakes)]; }
            set { vessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, value); }
        }

        /// <summary>
        /// Returns true if the specified action group is enabled
        /// </summary>
        [KRPCProcedure]
        public bool GetActionGroup (int grp)
        {
            return vessel.ActionGroups.groups [BaseAction.GetGroupIndex (Utils.GetActionGroup (grp))];
        }

        /// <summary>
        /// Sets whether the specified action group should be enabled
        /// </summary>
        [KRPCProcedure]
        public void SetActionGroup (int grp, bool value)
        {
            vessel.ActionGroups.SetGroup (Utils.GetActionGroup (grp), value);
        }
        // FIXME: what if vessel is not the active vessel?
        /// <summary>
        /// Throttle setting of the active vessel. Should be between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public float Throttle {
            get { return FlightInputHandler.state.mainThrottle; }
            set { FlightInputHandler.state.mainThrottle = value; }
        }

        [KRPCProperty]
        public float X {
            get { return PilotAddon.X; }
            set { PilotAddon.X = value; }
        }

        [KRPCProperty]
        public float Y {
            get { return PilotAddon.Y; }
            set { PilotAddon.Y = value; }
        }

        [KRPCProperty]
        public float Z {
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
    }
}

