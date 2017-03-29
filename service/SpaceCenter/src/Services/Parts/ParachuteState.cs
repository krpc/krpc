using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The state of a parachute. See <see cref="Parachute.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ParachuteState
    {
        /// <summary>
        /// The parachute is safely tucked away inside its housing.
        /// </summary>
        Stowed,
        /// <summary>
        /// The parachute is armed for deployment. (RealChutes only)
        /// </summary>
        Armed,
        /// <summary>
        /// The parachute is still stowed, but ready to semi-deploy.
        /// (Stock parachutes only)
        /// </summary>
        Active,
        /// <summary>
        /// The parachute has been deployed and is providing some drag,
        /// but is not fully deployed yet. (Stock parachutes only)
        /// </summary>
        SemiDeployed,
        /// <summary>
        /// The parachute is fully deployed.
        /// </summary>
        Deployed,
        /// <summary>
        /// The parachute has been cut.
        /// </summary>
        Cut
    }
}
