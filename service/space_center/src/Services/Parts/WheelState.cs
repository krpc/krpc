using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a wheel. See <see cref="Wheel.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum WheelState
    {
        /// <summary>
        /// Wheel is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Wheel is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Wheel is being deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Wheel is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Wheel is broken.
        /// </summary>
        Broken
    }
}
