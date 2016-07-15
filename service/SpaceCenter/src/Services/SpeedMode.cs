using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The mode of the speed reported in the navball.
    /// See <see cref="Control.SpeedMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SpeedMode
    {
        /// <summary>
        /// Speed is relative to the vessel's orbit.
        /// </summary>
        Orbit,
        /// <summary>
        /// Speed is relative to the surface of the body being orbited.
        /// </summary>
        Surface,
        /// <summary>
        /// Speed is relative to the current target.
        /// </summary>
        Target
    }
}
