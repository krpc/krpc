using UnityEngine;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSP.UI.Screens;

namespace LaunchControl {

    /// <summary>
    /// Service for staging vessels and controlling their throttle.
    /// </summary>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class LaunchControl {

        /// <summary>
        /// The current throttle setting for the active vessel, between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public static float Throttle {
            get { return FlightInputHandler.state.mainThrottle; }
            set { FlightInputHandler.state.mainThrottle = value; }
        }

        /// <summary>
        /// Activate the next stage in the vessel.
        /// </summary>
        [KRPCProcedure]
        public static void ActivateStage ()
        {
            StageManager.ActivateNextStage ();
        }
    }
}
