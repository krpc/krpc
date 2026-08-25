using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A contract. Can be accessed using <see cref="SpaceCenter.ContractManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Contract : Equatable<Contract>, IGameObjectState
    {
        // The guid the game gives a contract, which it writes into the save and reads back,
        // so it names the contract across a load. The contract system builds new contract
        // objects for a game it loads, so holding one would leave this reading a contract
        // out of a game state that is no longer loaded.
        readonly Guid contractGuid;

        /// <summary>
        /// Create a contract object from a KSP contract.
        /// </summary>
        public Contract(Contracts.Contract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            contractGuid = contract.ContractGuid;
        }

        /// <summary>
        /// The KSP contract, found again from the guid that names it.
        /// </summary>
        public Contracts.Contract InternalContract
        {
            get
            {
                var contract = Find();
                if (contract == null)
                    throw NotResolvable();
                return contract;
            }
        }

        /// <summary>
        /// The state of the contract. It is live while the contract system lists it,
        /// running or finished, and destroyed once a running system stops listing it. A
        /// game with no contract system leaves the contract dormant.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                if (Contracts.ContractSystem.Instance == null)
                    return GameObjectState.Dormant;
                return Find() != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The contract the game currently has under the guid, or null if it has none.
        /// A finished contract is still a contract this can stand for, and the game's own
        /// lookup by guid searches the running ones only, so both lists are searched.
        /// </summary>
        internal Contracts.Contract Find()
        {
            var system = Contracts.ContractSystem.Instance;
            if (system == null)
                return null;
            return FindIn(system.Contracts) ?? FindIn(system.ContractsFinished);
        }

        Contracts.Contract FindIn(IList<Contracts.Contract> contracts)
        {
            if (contracts == null)
                return null;
            for (var i = 0; i < contracts.Count; i++)
            {
                var contract = contracts[i];
                if (contract != null && contract.ContractGuid == contractGuid)
                    return contract;
            }
            return null;
        }

        /// <summary>
        /// The error to raise when no contract answers to the guid, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException(
                    "The contract no longer exists, as the game no longer has a contract with its id.");
            return new InvalidOperationException(
                "The contract is not loaded, as the game has no contract system running. " +
                "It can be used again once the game does.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Contract other)
        {
            return !ReferenceEquals(other, null) && contractGuid == other.contractGuid;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return contractGuid.GetHashCode();
        }

        /// <summary>
        /// Type of the contract.
        /// </summary>
        [KRPCProperty]
        public string Type {
            get { return InternalContract.GetType ().ToString (); }
        }

        /// <summary>
        /// Title of the contract.
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return InternalContract.Title; }
        }

        /// <summary>
        /// Description of the contract.
        /// </summary>
        [KRPCProperty]
        public string Description {
            get { return InternalContract.Description; }
        }

        /// <summary>
        /// Notes for the contract.
        /// </summary>
        [KRPCProperty]
        public string Notes {
            get { return InternalContract.Notes ?? string.Empty; }
        }

        /// <summary>
        /// Synopsis for the contract.
        /// </summary>
        [KRPCProperty]
        public string Synopsis {
            get { return InternalContract.Synopsys ?? string.Empty; }
        }

        /// <summary>
        /// Keywords for the contract.
        /// </summary>
        [KRPCProperty]
        public IList<string> Keywords {
            get { return InternalContract.Keywords; }
        }

        /// <summary>
        /// State of the contract.
        /// </summary>
        [KRPCProperty]
        public ContractState State {
            get { return InternalContract.ContractState.ToContractState(); }
        }

        /// <summary>
        /// Whether the contract is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return InternalContract.ContractState == Contracts.Contract.State.Active; }
        }

        /// <summary>
        /// Whether the contract has been failed.
        /// </summary>
        [KRPCProperty]
        public bool Failed {
            get { return InternalContract.ContractState == Contracts.Contract.State.Failed; }
        }

        /// <summary>
        /// Whether the contract has been seen.
        /// </summary>
        [KRPCProperty]
        public bool Seen {
            get {
                return InternalContract.ContractViewed == Contracts.Contract.Viewed.Seen ||
                       InternalContract.ContractViewed == Contracts.Contract.Viewed.Read;
            }
        }

        /// <summary>
        /// Whether the contract has been read.
        /// </summary>
        [KRPCProperty]
        public bool Read {
            get { return InternalContract.ContractViewed == Contracts.Contract.Viewed.Read; }
        }

        /// <summary>
        /// Whether the contract can be canceled.
        /// </summary>
        [KRPCProperty]
        public bool CanBeCanceled {
            get { return InternalContract.CanBeCancelled(); }
        }

        /// <summary>
        /// Whether the contract can be declined.
        /// </summary>
        [KRPCProperty]
        public bool CanBeDeclined {
            get { return InternalContract.CanBeDeclined(); }
        }

        /// <summary>
        /// Whether the contract can be failed.
        /// </summary>
        [KRPCProperty]
        public bool CanBeFailed {
            get { return InternalContract.CanBeFailed (); }
        }

        /// <summary>
        /// Cancel an active contract.
        /// </summary>
        [KRPCMethod]
        public void Cancel ()
        {
            InternalContract.Cancel ();
        }

        /// <summary>
        /// Accept an offered contract.
        /// </summary>
        [KRPCMethod]
        public void Accept ()
        {
            InternalContract.Accept ();
        }

        /// <summary>
        /// Decline an offered contract.
        /// </summary>
        [KRPCMethod]
        public void Decline()
        {
            InternalContract.Decline();
        }

        /// <summary>
        /// Funds received when accepting the contract.
        /// </summary>
        [KRPCProperty]
        public double FundsAdvance {
            get {
                return InternalContract.FundsAdvance;
            }
        }

        /// <summary>
        /// Funds received on completion of the contract.
        /// </summary>
        [KRPCProperty]
        public double FundsCompletion {
            get {
                return InternalContract.FundsCompletion;
            }
        }

        /// <summary>
        /// Funds lost if the contract is failed.
        /// </summary>
        [KRPCProperty]
        public double FundsFailure {
            get {
                return InternalContract.FundsFailure;
            }
        }

        /// <summary>
        /// Reputation gained on completion of the contract.
        /// </summary>
        [KRPCProperty]
        public double ReputationCompletion {
            get {
                return InternalContract.ReputationCompletion;
            }
        }

        /// <summary>
        /// Reputation lost if the contract is failed.
        /// </summary>
        [KRPCProperty]
        public double ReputationFailure {
            get {
                return InternalContract.ReputationFailure;
            }
        }

        /// <summary>
        /// Science gained on completion of the contract.
        /// </summary>
        [KRPCProperty]
        public double ScienceCompletion {
            get {
                return InternalContract.ScienceCompletion;
            }
        }

        /// <summary>
        /// Parameters for the contract.
        /// </summary>
        [KRPCProperty]
        public IList<ContractParameter> Parameters {
            get {
                var contract = InternalContract;
                var result = new List<ContractParameter>();
                for (int i = 0; i < contract.ParameterCount; i++)
                    result.Add(new ContractParameter(this, new [] { i }));
                return result;
            }
        }

        /// <summary>
        /// Universal time at which the contract was accepted, in seconds.
        /// Zero if the contract has not been accepted.
        /// </summary>
        [KRPCProperty]
        public double DateAccepted {
            get { return InternalContract.DateAccepted; }
        }

        /// <summary>
        /// Universal time by which the contract must be completed, in seconds.
        /// Zero if the contract has no deadline.
        /// </summary>
        [KRPCProperty]
        public double DateDeadline {
            get { return InternalContract.DateDeadline; }
        }

        /// <summary>
        /// Universal time at which the contract offer expires, in seconds.
        /// Zero if the contract has no expiry date.
        /// </summary>
        [KRPCProperty]
        public double DateExpire {
            get { return InternalContract.DateExpire; }
        }

        /// <summary>
        /// Universal time at which the contract was completed or failed, in seconds.
        /// Zero if the contract has not finished or KSP did not record a finish time.
        /// </summary>
        [KRPCProperty]
        public double DateFinished {
            get { return InternalContract.DateFinished; }
        }
    }
}
