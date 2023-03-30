using System;
using System.Runtime;
using KRPC.Service;

namespace KRPC.Utils
{
    static class GameScenesExtensions
    {
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
            default:
                if (Compatibility.GameSceneIsMissionBuilder(scene))
                    return GameScene.MissionBuilder;
                return GameScene.None;
            }
        }
    }
}
