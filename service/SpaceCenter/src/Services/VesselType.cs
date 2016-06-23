using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// See <see cref="Vessel.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum VesselType
    {
        /// <summary>
        /// Ship.
        /// </summary>
        Ship,
        /// <summary>
        /// Station.
        /// </summary>
        Station,
        /// <summary>
        /// Lander.
        /// </summary>
        Lander,
        /// <summary>
        /// Probe.
        /// </summary>
        Probe,
        /// <summary>
        /// Rover.
        /// </summary>
        Rover,
        /// <summary>
        /// Base.
        /// </summary>
        Base,
        /// <summary>
        /// Debris.
        /// </summary>
        Debris
    }
}
