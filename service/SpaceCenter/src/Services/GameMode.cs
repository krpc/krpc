using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// The game mode.
    /// Returned by <see cref="GameMode"/>
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum GameMode
    {
        /// <summary>
        /// Sandbox mode.
        /// </summary>
        Sandbox,
        /// <summary>
        /// Career mode.
        /// </summary>
        Career,
        /// <summary>
        /// Science career mode.
        /// </summary>
        Science,
        /// <summary>
        /// Science sandbox mode.
        /// </summary>
        ScienceSandbox,
        /// <summary>
        /// Mission mode.
        /// </summary>
        Mission,
        /// <summary>
        /// Mission builder mode.
        /// </summary>
        MissionBuilder,
        /// <summary>
        /// Scenario mode.
        /// </summary>
        Scenario,
        /// <summary>
        /// Scenario mode that cannot be resumed.
        /// </summary>
        ScenarioNonResumable
    }
}
