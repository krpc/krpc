using KRPC.Service.Attributes;
using KRPC.Schema.Control;
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

        [KRPCProcedure]
        public static void SetControlInputs (ControlInputs controls)
        {
            PilotAddon.SetControlInputs (controls);
        }

        [KRPCProcedure]
        public static void ActivateNextStage ()
        {
            Staging.ActivateNextStage ();
        }
    }
}

