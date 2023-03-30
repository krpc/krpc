using System;
using KRPC.Service.Attributes;

namespace KRPC.RemoteTech
{
    /// <summary>
    /// The type of object an antenna is targetting.
    /// See <see cref="Antenna.Target"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "RemoteTech")]
    public enum Target
    {
        /// <summary>
        /// The active vessel.
        /// </summary>
        ActiveVessel,
        /// <summary>
        /// A celestial body.
        /// </summary>
        CelestialBody,
        /// <summary>
        /// A ground station.
        /// </summary>
        GroundStation,
        /// <summary>
        /// A specific vessel.
        /// </summary>
        Vessel,
        /// <summary>
        /// No target.
        /// </summary>
        None
    }
}
