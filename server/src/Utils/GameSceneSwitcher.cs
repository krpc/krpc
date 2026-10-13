using System;
using KRPC.Service;
using PublicGameScene = KRPC.Service.KRPC.GameScene;

namespace KRPC.Utils
{
    /// <summary>
    /// Switches the game to a given scene, and tracks which space center
    /// facility UI is open. Setting KRPC.GameScene calls LoadScene via
    /// the CallContext.LoadScene delegate.
    /// </summary>
    static class GameSceneSwitcher
    {
        static Action pendingLoad;

        internal static void LoadScene (PublicGameScene scene)
        {
            var current = CallContext.GameScene;
            switch (scene) {
            case PublicGameScene.SpaceCenter:
                if ((current & GameScene.SpaceCenter) != 0)
                    CloseOpenFacility (current);
                else
                    Defer (() => HighLogic.LoadScene (GameScenes.SPACECENTER));
                break;
            case PublicGameScene.Flight:
                if ((current & GameScene.Flight) != 0)
                    break;
                ResumeFlight ();
                break;
            case PublicGameScene.TrackingStation:
                if ((current & GameScene.TrackingStation) != 0)
                    break;
                Defer (() => HighLogic.LoadScene (GameScenes.TRACKSTATION));
                break;
            case PublicGameScene.EditorVAB:
                StartEditor (current, GameScene.EditorVAB, EditorFacility.VAB);
                break;
            case PublicGameScene.EditorSPH:
                StartEditor (current, GameScene.EditorSPH, EditorFacility.SPH);
                break;
            case PublicGameScene.AstronautComplex:
                OpenFacility (current, GameScene.AstronautComplex,
                    GameEvents.onGUIAstronautComplexSpawn,
                    CanGoTo (p => p.CanGoToAstronautC));
                break;
            case PublicGameScene.MissionControl:
                OpenFacility (current, GameScene.MissionControl,
                    GameEvents.onGUIMissionControlSpawn,
                    CanGoTo (p => p.CanGoToMissionControl) &&
                    Contracts.ContractSystem.Instance != null);
                break;
            case PublicGameScene.ResearchAndDevelopment:
                OpenFacility (current, GameScene.ResearchAndDevelopment,
                    GameEvents.onGUIRnDComplexSpawn,
                    CanGoTo (p => p.CanGoToRnD) &&
                    ResearchAndDevelopment.Instance != null);
                break;
            case PublicGameScene.Administration:
                OpenFacility (current, GameScene.Administration,
                    GameEvents.onGUIAdministrationFacilitySpawn,
                    CanGoTo (p => p.CanGoToAdmin) &&
                    Strategies.StrategySystem.Instance != null);
                break;
            case PublicGameScene.MissionBuilder:
                throw new InvalidOperationException ("The mission builder cannot be entered by setting the game scene");
            default:
                throw new ArgumentException ("Unknown game scene");
            }
        }

        /// <summary>
        /// Runs the scene load requested by the last call to <see cref="LoadScene" />.
        /// Called from the addon's Update.
        /// </summary>
        internal static void Update ()
        {
            var load = pendingLoad;
            pendingLoad = null;
            if (load != null)
                load ();
        }

        /// <summary>
        /// Queues a scene load for the next Update, where the game's own buttons load
        /// scenes. RPCs run in FixedUpdate. A load requested there is followed by
        /// FlightGlobals.FixedUpdate in the same physics step, which sets FlightGlobals.ready
        /// again. The flag then stays set in the next scene with no active vessel.
        /// Leaving flight saves the game first.
        /// </summary>
        static void Defer (Action load)
        {
            if ((CallContext.GameScene & GameScene.Flight) != 0)
                load = LeaveFlight (load);
            pendingLoad = load;
        }

        /// <summary>
        /// Saves the game before leaving flight, as the game's own leave buttons do.
        /// When the game cannot be saved, e.g. while under acceleration, we leave
        /// without saving, as the game's "Leave Anyway" option does.
        /// </summary>
        static Action LeaveFlight (Action load)
        {
            return () => {
                var status = FlightGlobals.ActiveVessel == null ?
                    ClearToSaveStatus.CLEAR : FlightGlobals.ClearToSave (false);
                if (status == ClearToSaveStatus.CLEAR)
                    GamePersistence.SaveGame ("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                else
                    Logger.WriteLine ("Leaving flight without saving, as the game cannot be saved (" + status + ")", Logger.Severity.Warning);
                load ();
            };
        }

        /// <summary>
        /// Opens the given editor. Moving between the two editors keeps the vessel that
        /// is being constructed, as the game's own switch button does.
        /// </summary>
        static void StartEditor (GameScene current, GameScene editor, EditorFacility facility)
        {
            if ((current & editor) != 0)
                return;
            if ((current & GameScene.Editor) == 0) {
                Defer (() => EditorDriver.StartEditor (facility));
                return;
            }
            // Loading the editor scene while it is already the loaded scene leaves the
            // flight camera unable to target a vessel for the rest of the session. The
            // game's own switch button restarts the editor in place, which also carries
            // the vessel being constructed across. There are only two editors, so a switch
            // always lands on the one wanted
            var driver = EditorDriver.fetch;
            var logic = EditorLogic.fetch;
            if (driver == null || logic == null || driver.restartingEditor)
                throw new InvalidOperationException (
                    "The editor is still starting up and cannot be switched yet");
            Defer (logic.SwitchEditor);
        }

        static void ResumeFlight ()
        {
            var game = HighLogic.CurrentGame;
            var flightState = game == null ? null : game.flightState;
            var vesselIdx = flightState == null ? -1 : flightState.activeVesselIdx;
            if (flightState == null || vesselIdx < 0 || vesselIdx >= flightState.protoVessels.Count)
                throw new InvalidOperationException ("No active vessel to resume");
            // Save and reload the game state before entering flight, as KSP
            // does when resuming a flight from the space center
            Defer (() => {
                GamePersistence.SaveGame ("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                FlightDriver.StartAndFocusVessel ("persistent", vesselIdx);
            });
        }

        static void OpenFacility (GameScene current, GameScene facility, EventVoid spawnEvent, bool available)
        {
            if ((current & facility) != 0)
                return;
            if ((current & GameScene.SpaceCenter) == 0)
                throw new InvalidOperationException (
                    "Facilities can only be opened from the space center scene");
            // Replicate the availability check that the in-game building click handlers
            // apply (e.g. MissionControlBuilding.OnClicked). A facility whose backing
            // system is disabled in the current game mode, such as mission control, R&D
            // and administration in a sandbox game, cannot be opened. Firing its spawn
            // event regardless makes KSP throw
            if (!available)
                throw new InvalidOperationException (
                    "This facility is not available in the current game");
            CloseOpenFacility (current);
            spawnEvent.Fire ();
        }

        /// <summary>
        /// Whether the given predicate over the current game's space center
        /// parameters holds. Used to check facility availability. Returns false
        /// if there is no current game.
        /// </summary>
        static bool CanGoTo (Func<GameParameters.SpaceCenterParams, bool> predicate)
        {
            var game = HighLogic.CurrentGame;
            if (game == null || game.Parameters == null)
                return false;
            return predicate (game.Parameters.SpaceCenter);
        }

        static void CloseOpenFacility (GameScene current)
        {
            if ((current & GameScene.AstronautComplex) != 0)
                GameEvents.onGUIAstronautComplexDespawn.Fire ();
            else if ((current & GameScene.MissionControl) != 0)
                GameEvents.onGUIMissionControlDespawn.Fire ();
            else if ((current & GameScene.ResearchAndDevelopment) != 0)
                GameEvents.onGUIRnDComplexDespawn.Fire ();
            else if ((current & GameScene.Administration) != 0)
                GameEvents.onGUIAdministrationFacilityDespawn.Fire ();
        }

        /// <summary>
        /// Called when a facility UI is opened. Reports the facility via the
        /// current game scene, alongside the space center scene itself.
        /// </summary>
        internal static void SetFacility (GameScene facility)
        {
            if ((CallContext.GameScene & GameScene.SpaceCenter) != 0)
                CallContext.GameScene = GameScene.SpaceCenter | facility;
        }

        /// <summary>
        /// Called when a facility UI is closed.
        /// </summary>
        internal static void ClearFacility (GameScene facility)
        {
            if ((CallContext.GameScene & facility) != 0)
                CallContext.GameScene = GameScene.SpaceCenter;
        }
    }
}
