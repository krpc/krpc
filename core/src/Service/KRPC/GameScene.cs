using System;
using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The game scene. See <see cref="KRPC.GameScene"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KRPC")]
    public enum GameScene
    {
        /// <summary>
        /// The game scene showing the Kerbal Space Center buildings.
        /// </summary>
        SpaceCenter,
        /// <summary>
        /// The game scene showing a vessel in flight (or on the launchpad/runway).
        /// </summary>
        Flight,
        /// <summary>
        /// The tracking station.
        /// </summary>
        TrackingStation,
        /// <summary>
        /// The Vehicle Assembly Building.
        /// </summary>
        EditorVAB,
        /// <summary>
        /// The Space Plane Hangar.
        /// </summary>
        EditorSPH,
        /// <summary>
        /// The mission builder.
        /// </summary>
        MissionBuilder,
        /// <summary>
        /// The astronaut complex. This is a pseudo-scene, shown when the
        /// astronaut complex facility is open within the space center scene.
        /// </summary>
        AstronautComplex,
        /// <summary>
        /// Mission control. This is a pseudo-scene, shown when the
        /// mission control facility is open within the space center scene.
        /// </summary>
        MissionControl,
        /// <summary>
        /// Research and development. This is a pseudo-scene, shown when the
        /// research and development facility is open within the space center scene.
        /// </summary>
        ResearchAndDevelopment,
        /// <summary>
        /// The administration facility. This is a pseudo-scene, shown when the
        /// administration facility is open within the space center scene.
        /// </summary>
        Administration
    }
}
