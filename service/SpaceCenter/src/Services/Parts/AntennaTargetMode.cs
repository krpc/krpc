using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The kind of target a RealAntennas antenna points at. See <see cref="Antenna.TargetMode"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AntennaTargetMode
    {
        /// <summary>
        /// The antenna has no target. Omnidirectional antennas have none.
        /// </summary>
        None,
        /// <summary>
        /// A vessel. See <see cref="Antenna.TargetVessel"/>.
        /// </summary>
        Vessel,
        /// <summary>
        /// The center of a body. See <see cref="Antenna.TargetBody"/>.
        /// </summary>
        Body,
        /// <summary>
        /// A position on or above a body. See <see cref="Antenna.SetTargetPosition"/>.
        /// </summary>
        Position,
        /// <summary>
        /// An azimuth and elevation from the vessel's position. See
        /// <see cref="Antenna.SetTargetAzimuthElevation"/>.
        /// </summary>
        AzimuthElevation,
        /// <summary>
        /// A direction relative to the vessel's orbital velocity. See
        /// <see cref="Antenna.SetTargetOrbitRelative"/>.
        /// </summary>
        OrbitRelative
    }
}
