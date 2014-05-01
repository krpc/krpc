using UnityEngine;

namespace TestingTools
{
    // Loads a save called "default" and switches to the first active vessel
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public class AutoLoadPersistentSaveOnStartup : MonoBehaviour
    {
        static bool hasRun = false;

        public void Start ()
        {
            if (!hasRun) {
                HighLogic.SaveFolder = "default";
                var game = GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible) {
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
