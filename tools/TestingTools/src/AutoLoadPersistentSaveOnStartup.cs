using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace TestingTools
{
    /// <summary>
    /// Addon that loads a save called "default" and switches to the first active vessel.
    /// </summary>
    [KSPAddon (KSPAddon.Startup.MainMenu, false)]
    public sealed class AutoLoadPersistentSaveOnStartup : MonoBehaviour
    {
        /// <summary>
        /// Whether the addon has been run.
        /// </summary>
        public static bool HasRun { get; private set; }

        /// <summary>
        /// Start the addon.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public void Start ()
        {
            if (!HasRun) {
                HighLogic.SaveFolder = "default";
                var game = GamePersistence.LoadGame ("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible) {
                    HighLogic.CurrentGame = game;
                    HighLogic.CurrentGame.Load();

                    // Get the vessel index of the first vessel
                    bool foundVessel = false;
                    int vesselIdx = 0;
                    foreach (var vessel in game.flightState.protoVessels) {
                        if (vessel.vesselType != VesselType.SpaceObject) {
                            foundVessel = true;
                            break;
                        }
                        vesselIdx++;
                    }
                    if (!foundVessel)
                        return;

                    // Load the vessel
                    FlightDriver.StartAndFocusVessel (game, vesselIdx);
                    HasRun = true;
                }
            }
        }
    }
}
