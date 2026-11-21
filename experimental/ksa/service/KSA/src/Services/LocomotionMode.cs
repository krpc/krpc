using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// How a kitten on EVA is moving. See <see cref="Kitten.LocomotionMode" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum LocomotionMode
    {
        /// <summary>
        /// Flying with the maneuvering unit, away from any surface.
        /// </summary>
        MMU,
        /// <summary>
        /// Standing or walking on a surface.
        /// </summary>
        Grounded,
        /// <summary>
        /// In the air above a surface, such as during a jump.
        /// </summary>
        Airborne,
        /// <summary>
        /// Tumbling after a fall or an impact.
        /// </summary>
        Tumbling,
        /// <summary>
        /// Getting back up after tumbling.
        /// </summary>
        Righting,
        /// <summary>
        /// Holding a ladder or grab rail.
        /// </summary>
        Ladder,
        /// <summary>
        /// Swimming in an ocean.
        /// </summary>
        Swimming
    }
}
