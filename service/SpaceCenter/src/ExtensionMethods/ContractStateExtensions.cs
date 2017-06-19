using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ContractStateExtensions
    {
        [SuppressMessage("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        [SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public static ContractState ToContractState(this Contracts.Contract.State state)
        {
            switch (state)
            {
                case Contracts.Contract.State.Active:
                    return ContractState.Active;
                case Contracts.Contract.State.Cancelled:
                    return ContractState.Canceled;
                case Contracts.Contract.State.Completed:
                    return ContractState.Completed;
                case Contracts.Contract.State.DeadlineExpired:
                    return ContractState.DeadlineExpired;
                case Contracts.Contract.State.Declined:
                    return ContractState.Declined;
                case Contracts.Contract.State.Failed:
                    return ContractState.Failed;
                case Contracts.Contract.State.Generated:
                    return ContractState.Generated;
                case Contracts.Contract.State.Offered:
                    return ContractState.Offered;
                case Contracts.Contract.State.OfferExpired:
                    return ContractState.OfferExpired;
                case Contracts.Contract.State.Withdrawn:
                    return ContractState.Withdrawn;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
    }
}
