using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of an antenna. See <see cref="Antenna.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum AntennaState
    {
        /// <summary>
        /// Antenna is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Antenna is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Antenna is being deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Antenna is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Antenna is broken.
        /// </summary>
        Broken
    }
}
