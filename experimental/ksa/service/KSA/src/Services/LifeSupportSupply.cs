using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A supply the crew needs. See <see cref="LifeSupport.LimitingSupply" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum LifeSupportSupply
    {
        /// <summary>
        /// Oxygen to breathe.
        /// </summary>
        Air,
        /// <summary>
        /// Lithium hydroxide canisters, which scrub carbon dioxide from the air.
        /// </summary>
        Scrubber,
        /// <summary>
        /// Drinking water.
        /// </summary>
        Water,
        /// <summary>
        /// Meals.
        /// </summary>
        Food
    }
}
