using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a landing gear. See <see cref="LandingGear.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum LandingGearState
    {
        /// <summary>
        /// Landing gear is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Landing gear is fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Landing gear is being deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Landing gear is being retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Landing gear is broken.
        /// </summary>
        Broken
    }
}
