using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A contract. Can be accessed using <see cref="SpaceCenter.ContractManager"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class Contract : Equatable<Contract>
    {
        /// <summary>
        /// Create a contract object from a KSP contract.
        /// </summary>
        public Contract(Contracts.Contract contract)
        {
            InternalContract = contract;
        }

        /// <summary>
        /// The KSP contract.
        /// </summary>
        public Contracts.Contract InternalContract { get; private set; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(Contract other)
        {
            return !ReferenceEquals(other, null) && InternalContract.ContractID == other.InternalContract.ContractID;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalContract.ContractID.GetHashCode();
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
                    result.Add(new ContractParameter(contract.GetParameter(i)));
                return result;
            }
        }

        //TODO: times and dates
        //TODO: contract parameters
    }
}
