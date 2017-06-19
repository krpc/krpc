using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The type of a vessel.
    /// See <see cref="Vessel.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum VesselType
    {
        /// <summary>
        /// Base.
        /// </summary>
        Base,
        /// <summary>
        /// Debris.
        /// </summary>
        Debris,
        /// <summary>
        /// Lander.
        /// </summary>
        Lander,
        /// <summary>
        /// Plane.
        /// </summary>
        Plane,
        /// <summary>
        /// Probe.
        /// </summary>
        Probe,
        /// <summary>
        /// Relay.
        /// </summary>
        Relay,
        /// <summary>
        /// Rover.
        /// </summary>
        Rover,
        /// <summary>
        /// Ship.
        /// </summary>
        Ship,
        /// <summary>
        /// Station.
        /// </summary>
        Station
    }
}
