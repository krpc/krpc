using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// What a mission does at its destination. See <see cref="Mission.Arrival" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum MissionArrival
    {
        /// <summary>
        /// Flies past it.
        /// </summary>
        Flyby,
        /// <summary>
        /// Orbits it.
        /// </summary>
        Orbit,
        /// <summary>
        /// Lands on it.
        /// </summary>
        Land
    }
}
