using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The behavior of the SAS auto-pilot. See <see cref="AutoPilot.SASMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SASMode
    {
        /// <summary>
        /// Stability assist mode. Dampen out any rotation.
        /// </summary>
        StabilityAssist,
        /// <summary>
        /// Point in the burn direction of the next maneuver node.
        /// </summary>
        Maneuver,
        /// <summary>
        /// Point in the prograde direction.
        /// </summary>
        Prograde,
        /// <summary>
        /// Point in the retrograde direction.
        /// </summary>
        Retrograde,
        /// <summary>
        /// Point in the orbit normal direction.
        /// </summary>
        Normal,
        /// <summary>
        /// Point in the orbit anti-normal direction.
        /// </summary>
        AntiNormal,
        /// <summary>
        /// Point in the orbit radial direction.
        /// </summary>
        Radial,
        /// <summary>
        /// Point in the orbit anti-radial direction.
        /// </summary>
        AntiRadial,
        /// <summary>
        /// Point in the direction of the current target.
        /// </summary>
        Target,
        /// <summary>
        /// Point away from the current target.
        /// </summary>
        AntiTarget
    }
}
