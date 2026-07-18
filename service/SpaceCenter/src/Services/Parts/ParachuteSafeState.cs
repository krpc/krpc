using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The safety state of deploying a parachute. See <see cref="Parachute.SafeState"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ParachuteSafeState
    {
        /// <summary>
        /// The parachute is safe to deploy.
        /// </summary>
        Safe,
        /// <summary>
        /// Deploying the parachute is risky, and it may break off.
        /// </summary>
        Risky,
        /// <summary>
        /// The parachute is unsafe to deploy, and will break off.
        /// </summary>
        Unsafe,
        /// <summary>
        /// The safety state is not available, because the vessel is not in an
        /// atmosphere or the parachute has already been deployed or cut.
        /// </summary>
        None
    }
}
