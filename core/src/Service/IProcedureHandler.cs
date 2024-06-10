using System;
using System.Collections.Generic;

namespace KRPC.Service
{
    /// <summary>
    /// Use to invoke the method that implements an RPC
    /// </summary>
    public interface IProcedureHandler
    {
        /// <summary>
        /// Invoke the RPC with the given arguments.
        /// If the RPC is a class method, the first argument should
        /// be an instance of the class.
        /// </summary>
        object Invoke (params object[] arguments);

        /// <summary>
        /// Information about the parameters of the method
        /// </summary>
        IEnumerable<ProcedureParameter> Parameters { get; }

        /// <summary>
        /// Return type of the method
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Whether the method could return null
        /// </summary>
        bool ReturnIsNullable { get; }
    }
}
