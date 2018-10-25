using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace KRPC.Service
{
    /// <summary>
    /// KSP game scenes
    /// </summary>
    [Flags]
    [Serializable]
    [SuppressMessage ("Gendarme.Rules.Design", "FlagsShouldNotDefineAZeroValueRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UsePluralNameInEnumFlagsRule")]
    public enum GameScene
    {
        /// <summary>
        /// No game scene.
        /// </summary>
        None = 0,

        /// <summary>
        /// Inherit the game scene from the enclosing service or class.
        /// </summary>
        Inherit = 0,

        /// <summary>
        /// The space center overview
        /// </summary>
        SpaceCenter = 1 << 0,

        /// <summary>
        /// When piloting a vessel
        /// </summary>
        Flight = 1 << 1,

        /// <summary>
        /// The tracking station
        /// </summary>
        TrackingStation = 1 << 2,

        /// <summary>
        /// The VAB editor
        /// </summary>
        EditorVAB = 1 << 3,

        /// <summary>
        /// The SPH editor
        /// </summary>
        EditorSPH = 1 << 4,

        /// <summary>
        /// The VAB or SPH editors
        /// </summary>
        Editor = EditorSPH | EditorVAB,

        /// <summary>
        /// The mission builder
        /// </summary>
        MissionBuilder = 1 << 5,

        /// <summary>
        /// All game scenes
        /// </summary>
        All = ~0
    }

    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    static class GameSceneUtils {
        public static string Name(GameScene scene) {
            return string.Join(", ", scene.ToString().Split(',').Where(x => x != "Inherit").Select(x => x.Trim()).ToArray());
        }

        public static IList<string> Serialize(GameScene scene) {
            IList<string> result = new List<string>();
            if ((scene & GameScene.SpaceCenter) != 0)
                result.Add("SPACE_CENTER");
            if ((scene & GameScene.Flight) != 0)
                result.Add("FLIGHT");
            if ((scene & GameScene.TrackingStation) != 0)
                result.Add("TRACKING_STATION");
            if ((scene & GameScene.EditorVAB) != 0)
                result.Add("EDITOR_VAB");
            if ((scene & GameScene.EditorSPH) != 0)
                result.Add("EDITOR_SPH");
            if ((scene & GameScene.MissionBuilder) != 0)
                result.Add("MISSION_BUILDER");
            return result;
        }
    };
}
