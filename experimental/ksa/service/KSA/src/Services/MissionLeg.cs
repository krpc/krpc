using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A leg of a mission. See <see cref="HabitatZone.PlannedKittens" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum MissionLeg
    {
        /// <summary>
        /// The way out.
        /// </summary>
        Out,
        /// <summary>
        /// The stay at the destination.
        /// </summary>
        Stay,
        /// <summary>
        /// The way back.
        /// </summary>
        Back
    }
}
