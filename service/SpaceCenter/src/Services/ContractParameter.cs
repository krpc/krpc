using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A contract parameter. See <see cref="Contract.Parameters"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class ContractParameter : Equatable<ContractParameter>
    {
        /// <summary>
        /// Create a contract parameter object from a KSP contract parameter.
        /// </summary>
        public ContractParameter(Contracts.ContractParameter parameter)
        {
            InternalParameter = parameter;
        }

        /// <summary>
        /// The KSP contract parameter.
        /// </summary>
        public Contracts.ContractParameter InternalParameter { get; private set; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(ContractParameter other)
        {
            return !ReferenceEquals(other, null) && InternalParameter == other.InternalParameter;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return InternalParameter.GetHashCode();
        }

        /// <summary>
        /// Title of the parameter.
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return InternalParameter.Title ?? string.Empty; }
        }

        /// <summary>
        /// Notes for the parameter.
        /// </summary>
        [KRPCProperty]
        public string Notes
        {
            get { return InternalParameter.Notes; }
        }

        /// <summary>
        /// Child contract parameters.
        /// </summary>
        [KRPCProperty]
        public IList<ContractParameter> Children {
            get {
                var parameter = InternalParameter;
                var result = new List<ContractParameter> ();
                for (int i = 0; i < parameter.ParameterCount; i++)
                    result.Add(new ContractParameter(parameter.GetParameter(i)));
                return result;
            }
        }

        /// <summary>
        /// Whether the parameter has been completed.
        /// </summary>
        [KRPCProperty]
        public bool Completed {
            get { return InternalParameter.State == Contracts.ParameterState.Complete; }
        }

        /// <summary>
        /// Whether the parameter has been failed.
        /// </summary>
        [KRPCProperty]
        public bool Failed {
            get { return InternalParameter.State == Contracts.ParameterState.Failed; }
        }

        /// <summary>
        /// Whether the contract parameter is optional.
        /// </summary>
        [KRPCProperty]
        public bool Optional {
            get { return InternalParameter.Optional; }
        }

        /// <summary>
        /// Funds received on completion of the contract parameter.
        /// </summary>
        [KRPCProperty]
        public double FundsCompletion {
            get {
                return InternalParameter.FundsCompletion;
            }
        }

        /// <summary>
        /// Funds lost if the contract parameter is failed.
        /// </summary>
        [KRPCProperty]
        public double FundsFailure {
            get {
                return InternalParameter.FundsFailure;
            }
        }

        /// <summary>
        /// Reputation gained on completion of the contract parameter.
        /// </summary>
        [KRPCProperty]
        public double ReputationCompletion {
            get {
                return InternalParameter.ReputationCompletion;
            }
        }

        /// <summary>
        /// Reputation lost if the contract parameter is failed.
        /// </summary>
        [KRPCProperty]
        public double ReputationFailure {
            get {
                return InternalParameter.ReputationFailure;
            }
        }

        /// <summary>
        /// Science gained on completion of the contract parameter.
        /// </summary>
        [KRPCProperty]
        public double ScienceCompletion {
            get {
                return InternalParameter.ScienceCompletion;
            }
        }
    }
}
