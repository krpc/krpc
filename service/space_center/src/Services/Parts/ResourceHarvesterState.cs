using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a resource harvester. See <see cref="ResourceHarvester.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceHarvesterState
    {
        /// <summary>
        /// The drill is deploying.
        /// </summary>
        Deploying,
        /// <summary>
        /// The drill is deployed and ready.
        /// </summary>
        Deployed,
        /// <summary>
        /// The drill is retracting.
        /// </summary>
        Retracting,
        /// <summary>
        /// The drill is retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// The drill is running.
        /// </summary>
        Active
    }
}
