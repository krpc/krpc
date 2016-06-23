using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a radiator. <see cref="RadiatorState"/>
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum RadiatorState
    {
        /// <summary>
        /// Radiator is fully extended.
        /// </summary>
        Extended,
        /// <summary>
        /// Radiator is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Radiator is being extended.
        /// </summary>
        Extending,
        /// <summary>
        /// Radiator is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Radiator is being broken.
        /// </summary>
        Broken
    }
}
