using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Contracts manager.
    /// Obtained by calling <see cref="SpaceCenter.WaypointManager"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ContractManager : Equatable<ContractManager>
    {
        internal ContractManager ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ContractManager other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// A list of all contract types.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public HashSet<string> Types {
            get { return new HashSet<string> (Contracts.ContractSystem.ContractTypes.Select (x => x.ToString ())); }
        }

        /// <summary>
        /// A list of all contracts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Contract> AllContracts {
            get { return Enumerable.Concat(
                      Contracts.ContractSystem.Instance.Contracts.Select (x => new Contract (x)),
                      Contracts.ContractSystem.Instance.ContractsFinished.Select(x => new Contract(x))).ToList();
            }
        }

        /// <summary>
        /// A list of all active contracts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Contract> ActiveContracts {
            get { return Contracts.ContractSystem.Instance.GetCurrentActiveContracts<Contracts.Contract> ().Select (x => new Contract (x)).ToList (); }
        }

        /// <summary>
        /// A list of all offered, but unaccepted, contracts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Contract> OfferedContracts {
            get {
                return Contracts.ContractSystem.Instance.GetCurrentContracts<Contracts.Contract> (x => x.ContractState == Contracts.Contract.State.Offered)
                    .Select (x => new Contract (x)).ToList ();
            }
        }

        /// <summary>
        /// A list of all completed contracts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Contract> CompletedContracts {
            get {
                return Contracts.ContractSystem.Instance.GetCompletedContracts<Contracts.Contract> ()
                    .Select (x => new Contract (x)).ToList ();
            }
        }

        /// <summary>
        /// A list of all failed contracts.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Contract> FailedContracts {
            get {
                return Contracts.ContractSystem.Instance.GetCompletedContracts<Contracts.Contract>(x => x.ContractState == Contracts.Contract.State.Failed)
                    .Select(x => new Contract(x)).ToList();
            }
        }
    }
}
