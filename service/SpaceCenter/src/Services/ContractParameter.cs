using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// A contract parameter. See <see cref="Contract.Parameters"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class ContractParameter : Equatable<ContractParameter>, IGameObjectState
    {
        // The contract the parameter belongs to, and the indices that lead to it: a
        // contract's parameters are an ordered list and so are each parameter's own, so the
        // path names the parameter for as long as the contract exists. The game gives a
        // parameter nothing else to be named by, and builds new parameter objects whenever
        // it builds the contract.
        readonly Contract contract;
        readonly int[] path;

        /// <summary>
        /// Create a contract parameter object from a KSP contract parameter.
        /// </summary>
        public ContractParameter(Contracts.ContractParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
            var root = parameter.Root;
            if (root == null)
                throw new ArgumentException("Contract parameter does not belong to a contract");
            contract = new Contract(root);
            path = PathTo(root, parameter);
            if (path == null)
                throw new ArgumentException("Contract parameter was not found in its contract");
        }

        internal ContractParameter(Contract parameterContract, int[] parameterPath)
        {
            contract = parameterContract;
            path = parameterPath;
        }

        /// <summary>
        /// The KSP contract parameter, found again by walking the contract's parameters.
        /// </summary>
        public Contracts.ContractParameter InternalParameter
        {
            get
            {
                var parameter = Find();
                if (parameter == null)
                    throw NotResolvable();
                return parameter;
            }
        }

        /// <summary>
        /// The state of the parameter. It takes the state of its contract, and is destroyed
        /// once a live contract stops holding it.
        /// </summary>
        public GameObjectState GameObjectState
        {
            get
            {
                var state = contract.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return Find() != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The parameter the path leads to now, or null if it leads nowhere.
        /// </summary>
        Contracts.ContractParameter Find()
        {
            var current = contract.Find();
            if (current == null)
                return null;
            Contracts.ContractParameter parameter = null;
            foreach (var index in path)
            {
                var count = parameter == null ? current.ParameterCount : parameter.ParameterCount;
                if (index < 0 || index >= count)
                    return null;
                parameter = parameter == null
                    ? current.GetParameter(index) : parameter.GetParameter(index);
                if (parameter == null)
                    return null;
            }
            return parameter;
        }

        /// <summary>
        /// The indices that lead from the contract to the given parameter, or null if it is
        /// not one of the contract's.
        /// </summary>
        static int[] PathTo(Contracts.Contract contract, Contracts.ContractParameter parameter)
        {
            for (var i = 0; i < contract.ParameterCount; i++)
            {
                var found = PathTo(contract.GetParameter(i), parameter, new List<int> { i });
                if (found != null)
                    return found;
            }
            return null;
        }

        static int[] PathTo(
            Contracts.ContractParameter candidate,
            Contracts.ContractParameter parameter,
            List<int> pathToCandidate)
        {
            if (ReferenceEquals(candidate, parameter))
                return pathToCandidate.ToArray();
            for (var i = 0; i < candidate.ParameterCount; i++)
            {
                pathToCandidate.Add(i);
                var found = PathTo(candidate.GetParameter(i), parameter, pathToCandidate);
                if (found != null)
                    return found;
                pathToCandidate.RemoveAt(pathToCandidate.Count - 1);
            }
            return null;
        }

        /// <summary>
        /// The error to raise when the path leads nowhere, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException(
                    "The contract parameter no longer exists, as the contract it belongs to " +
                    "no longer has it.");
            return new InvalidOperationException(
                "The contract parameter is not loaded, as the game has no contract system " +
                "running. It can be used again once the game does.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(ContractParameter other)
        {
            if (ReferenceEquals(other, null) || !contract.Equals(other.contract) ||
                path.Length != other.path.Length)
                return false;
            for (var i = 0; i < path.Length; i++)
                if (path[i] != other.path[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = Hash.Of(contract);
            foreach (var index in path)
                hash = hash.And(index);
            return hash;
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
                    result.Add(new ContractParameter(contract, PathToChild(i)));
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

        /// <summary>
        /// The path that leads to the child of this parameter at the given index.
        /// </summary>
        int[] PathToChild(int index)
        {
            var result = new int[path.Length + 1];
            Array.Copy(path, result, path.Length);
            result[path.Length] = index;
            return result;
        }
    }
}
