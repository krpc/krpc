using System.Diagnostics.CodeAnalysis;
using KRPC.Service;

namespace KRPC.Utils
{
    static class GameScenesExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        internal static GameScene CurrentGameScene ()
        {
            var scene = HighLogic.LoadedScene;
            switch (scene) {
            case GameScenes.SPACECENTER:
                return GameScene.SpaceCenter;
            case GameScenes.FLIGHT:
                return GameScene.Flight;
            case GameScenes.TRACKSTATION:
                return GameScene.TrackingStation;
            case GameScenes.EDITOR:
                return EditorDriver.editorFacility == EditorFacility.VAB ?
                    GameScene.EditorVAB : GameScene.EditorSPH;
            case GameScenes.MISSIONBUILDER:
                return GameScene.MissionBuilder;
            default:
                return GameScene.None;
            }
        }
    }
}
