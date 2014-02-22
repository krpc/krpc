using KRPC.Service;
using KRPC.Schema.Control;

namespace KRPCServices
{
    [KRPCService]
    static public class Control
    {
        [KRPCProcedure]
        public static void EnableSAS ()
        {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, true);
        }

        [KRPCProcedure]
        public static void DisableSAS ()
        {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, false);
        }

        [KRPCProcedure]
        public static void EnableRCS ()
        {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, true);
        }

        [KRPCProcedure]
        public static void DisableRCS ()
        {
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, false);
        }

        [KRPCProcedure]
        public static void SetThrottle (float throttle)
        {
            FlightInputHandler.state.mainThrottle = throttle;
        }

        [KRPCProcedure]
        public static float GetThrottle ()
        {
            return FlightInputHandler.state.mainThrottle;
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

