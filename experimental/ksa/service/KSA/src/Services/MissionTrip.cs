using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Whether a mission comes back. See <see cref="Mission.Trip" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum MissionTrip
    {
        /// <summary>
        /// Goes and comes back.
        /// </summary>
        Return,
        /// <summary>
        /// Goes and stays.
        /// </summary>
        OneWay
    }
}
