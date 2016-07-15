using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The situation a vessel is in.
    /// See <see cref="Vessel.Situation"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum VesselSituation
    {
        /// <summary>
        /// Vessel is awaiting launch.
        /// </summary>
        PreLaunch,
        /// <summary>
        /// Vessel is orbiting a body.
        /// </summary>
        Orbiting,
        /// <summary>
        /// Vessel is on a sub-orbital trajectory.
        /// </summary>
        SubOrbital,
        /// <summary>
        /// Escaping.
        /// </summary>
        Escaping,
        /// <summary>
        /// Vessel is flying through an atmosphere.
        /// </summary>
        Flying,
        /// <summary>
        /// Vessel is landed on the surface of a body.
        /// </summary>
        Landed,
        /// <summary>
        /// Vessel has splashed down in an ocean.
        /// </summary>
        Splashed,
        /// <summary>
        /// Vessel is docked to another.
        /// </summary>
        Docked
    }
}
