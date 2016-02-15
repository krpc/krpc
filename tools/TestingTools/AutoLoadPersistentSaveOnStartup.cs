using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that loads a save called "default" and switches to the first active vessel.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public class AutoLoadPersistentSaveOnStartup : MonoBehaviour
    {
        static bool hasRun = false;

        /// <summary>
        /// Start the addon.
        /// </summary>
        public void Start ()
        {
            if (!hasRun) {
                HighLogic.SaveFolder = "default";
                var game = GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible) {
                    // Check there is a vessel
                    if (game.flightState.protoVessels.Count == 0)
                        return;
                    // Get the vessel index of the first non-asteroid
                    int vesselIdx = 0;
                    foreach (var vessel in game.flightState.protoVessels) {
                        if (vessel.vesselType != VesselType.SpaceObject)
                            break;
                        vesselIdx++;
                    }
                    // Load the vessel
                    FlightDriver.StartAndFocusVessel (game, vesselIdx);
                    hasRun = true;
                }
            }
        }
    }
}
