using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Whether a habitat zone has the supplies for a mission. See <see cref="MissionSupplies.Verdict" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum MissionSupplyVerdict
    {
        /// <summary>
        /// The zone has no crew during the mission.
        /// </summary>
        Unused,
        /// <summary>
        /// The supplies last the mission.
        /// </summary>
        Enough,
        /// <summary>
        /// A supply runs out before the end of the mission.
        /// </summary>
        Short
    }
}
