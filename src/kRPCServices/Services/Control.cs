using KRPC.Service.Attributes;
using KSP;

namespace KRPCServices.Services
{
    [KRPCService]
    static public class Control
    {
        /// <summary>
        /// SAS enabled/disabled for the active vessel
        /// </summary>
        [KRPCProperty]
        public static bool SAS {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        /// <summary>
        /// RCS enabled/disabled for the active vessel
        /// </summary>
        [KRPCProperty]
        public static bool RCS {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

        /// <summary>
        /// Landing gear/legs deployed/retracted for the active vessel
        /// </summary>
        [KRPCProperty]
        public static bool Gear {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Gear)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.Gear, value); }
        }

        /// <summary>
        /// Lights on/off for the active vessel
        /// </summary>
        [KRPCProperty]
        public static bool Lights {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Light)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.Light, value); }
        }

        /// <summary>
        /// Brakes on/off for the active vessel
        /// </summary>
        [KRPCProperty]
        public static bool Brakes {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.Brakes)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, value); }
        }

        /// <summary>
        /// Returns true if the specified action group is enabled
        /// </summary>
        [KRPCProcedure]
        public static bool GetActionGroup (int grp)
        {
            return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (Utils.GetActionGroup (grp))];
        }

        /// <summary>
        /// Sets whether the specified action group should be enabled
        /// </summary>
        [KRPCProcedure]
        public static void SetActionGroup (int grp, bool value)
        {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (Utils.GetActionGroup (grp), value);
        }

        /// <summary>
        /// Throttle setting of the active vessel. Should be between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public static float Throttle {
            get { return FlightInputHandler.state.mainThrottle; }
            set { FlightInputHandler.state.mainThrottle = value; }
        }

        [KRPCProperty]
        public static float X {
            get { return PilotAddon.X; }
            set { PilotAddon.X = value; }
        }

        [KRPCProperty]
        public static float Y {
            get { return PilotAddon.Y; }
            set { PilotAddon.Y = value; }
        }

        [KRPCProperty]
        public static float Z {
            get { return PilotAddon.Z; }
            set { PilotAddon.Z = value; }
        }

        [KRPCProperty]
        public static float Pitch {
            get { return PilotAddon.Pitch; }
            set { PilotAddon.Pitch = value; }
        }

        [KRPCProperty]
        public static float Roll {
            get { return PilotAddon.Roll; }
            set { PilotAddon.Roll = value; }
        }

        [KRPCProperty]
        public static float Yaw {
            get { return PilotAddon.Yaw; }
            set { PilotAddon.Yaw = value; }
        }

        [KRPCProcedure]
        public static void ActivateNextStage ()
        {
            Staging.ActivateNextStage ();
        }
    }
}

