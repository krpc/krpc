using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The cause of a crew event. See <see cref="CrewEvent.Hazard" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum CrewHazard
    {
        /// <summary>
        /// Running out of air.
        /// </summary>
        Air,
        /// <summary>
        /// Running out of carbon dioxide scrubber.
        /// </summary>
        Scrubber,
        /// <summary>
        /// Running out of water.
        /// </summary>
        Water,
        /// <summary>
        /// Running out of food.
        /// </summary>
        Food,
        /// <summary>
        /// The radiation dose.
        /// </summary>
        Radiation
    }
}
