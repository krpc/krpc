using KRPC.Service.Attributes;
using KSP;

namespace KRPCServices
{
    [KRPCService]
    static public class Control
    {
        [KRPCProperty]
        public static bool SAS {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.SAS)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, value); }
        }

        [KRPCProperty]
        public static bool RCS {
            get { return FlightGlobals.ActiveVessel.ActionGroups.groups [BaseAction.GetGroupIndex (KSPActionGroup.RCS)]; }
            set { FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, value); }
        }

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

