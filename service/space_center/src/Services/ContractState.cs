using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// The state of a contract. See <see cref="Contract.State"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum(Service = "SpaceCenter")]
    public enum ContractState
    {
        /// <summary>
        /// The contract is active.
        /// </summary>
        Active,
        /// <summary>
        /// The contract has been canceled.
        /// </summary>
        Canceled,
        /// <summary>
        /// The contract has been completed.
        /// </summary>
        Completed,
        /// <summary>
        /// The deadline for the contract has expired.
        /// </summary>
        DeadlineExpired,
        /// <summary>
        /// The contract has been declined.
        /// </summary>
        Declined,
        /// <summary>
        /// The contract has been failed.
        /// </summary>
        Failed,
        /// <summary>
        /// The contract has been generated.
        /// </summary>
        Generated,
        /// <summary>
        /// The contract has been offered to the player.
        /// </summary>
        Offered,
        /// <summary>
        /// The contract was offered to the player, but the offer expired.
        /// </summary>
        OfferExpired,
        /// <summary>
        /// The contract has been withdrawn.
        /// </summary>
        Withdrawn
    }
}
