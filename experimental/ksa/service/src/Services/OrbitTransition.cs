using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// How an orbit ends. See <see cref="Orbit.EndTransition" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum OrbitTransition
    {
        /// <summary>
        /// The orbit does not end.
        /// </summary>
        None,
        /// <summary>
        /// The orbit enters the sphere of influence of another body.
        /// </summary>
        Encounter,
        /// <summary>
        /// The orbit leaves the sphere of influence of the body being orbited.
        /// </summary>
        Escape,
        /// <summary>
        /// The orbit hits the surface of the body being orbited.
        /// </summary>
        Impact,
        /// <summary>
        /// The orbit ends at a planned burn.
        /// </summary>
        Burn
    }
}
