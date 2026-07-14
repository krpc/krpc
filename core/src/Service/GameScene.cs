using System;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.Service
{
    /// <summary>
    /// KSP game scenes
    /// </summary>
    [Flags]
    [Serializable]
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
        /// The astronaut complex facility, open within the space center scene
        /// </summary>
        AstronautComplex = 1 << 6,

        /// <summary>
        /// The mission control facility, open within the space center scene
        /// </summary>
        MissionControl = 1 << 7,

        /// <summary>
        /// The research and development facility, open within the space center scene
        /// </summary>
        ResearchAndDevelopment = 1 << 8,

        /// <summary>
        /// The administration facility, open within the space center scene
        /// </summary>
        Administration = 1 << 9,

        /// <summary>
        /// All game scenes
        /// </summary>
        All = ~0
    }

    static class GameSceneUtils {
        public static string Name(GameScene scene) {
            return string.Join(", ", scene.ToString().Split(new char[] { ',' }).Where(x => x != "Inherit").Select(x => x.Trim()).ToArray());
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
            if ((scene & GameScene.AstronautComplex) != 0)
                result.Add("ASTRONAUT_COMPLEX");
            if ((scene & GameScene.MissionControl) != 0)
                result.Add("MISSION_CONTROL");
            if ((scene & GameScene.ResearchAndDevelopment) != 0)
                result.Add("RESEARCH_AND_DEVELOPMENT");
            if ((scene & GameScene.Administration) != 0)
                result.Add("ADMINISTRATION");
            return result;
        }
    };
}
