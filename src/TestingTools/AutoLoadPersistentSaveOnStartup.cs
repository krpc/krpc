using UnityEngine;

namespace TestingTools
{
    // Loads a save called "default" and sets the first vessel active when the game loads
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public class AutoLoadPersistentSaveOnStartup : MonoBehaviour
    {
        static bool hasRun;

        public void Start ()
        {
            if (!hasRun) {
                HighLogic.SaveFolder = "default";
                var game = GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible) {
                    FlightDriver.StartAndFocusVessel (game, 0);
                    hasRun = true;
                }
            }
        }
    }
}
