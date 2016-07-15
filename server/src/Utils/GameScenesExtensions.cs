using System.Diagnostics.CodeAnalysis;
using KRPC.Service;

namespace KRPC.Utils
{
    static class GameScenesExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        internal static GameScene ToGameScene (this GameScenes scene)
        {
            switch (scene) {
            case GameScenes.SPACECENTER:
                return GameScene.SpaceCenter;
            case GameScenes.FLIGHT:
                return GameScene.Flight;
            case GameScenes.TRACKSTATION:
                return GameScene.TrackingStation;
            case GameScenes.EDITOR:
                return GameScene.Editor;
            default:
                return GameScene.None;
            }
        }
    }
}
