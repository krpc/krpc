using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The state of a deployable part. See <see cref="Deployable.State" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum DeployableState
    {
        /// <summary>
        /// Fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// Fully retracted.
        /// </summary>
        Retracted,
        /// <summary>
        /// Moving toward deployed.
        /// </summary>
        Deploying,
        /// <summary>
        /// Moving toward retracted.
        /// </summary>
        Retracting,
        /// <summary>
        /// Broken, so it can no longer move.
        /// </summary>
        Broken
    }
}
