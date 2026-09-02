using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// Use to invoke the method that implements an RPC
    /// </summary>
    public interface IProcedureHandler
    {
        /// <summary>
        /// Whether the RPC is a class instance method or a
        /// static/standalone procedure
        /// </summary>
        bool HasInstance { get; }

        /// <summary>
        /// Invoke the RPC with the given arguments.
        /// Pass the class instance in <paramref name="instance"/>
        /// for class methods, or null for static procedures.
        /// </summary>
        object Invoke (object instance, object[] arguments);

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

        /// <summary>
        /// The nullable positions inside the return type, each named by a path
        /// </summary>
        IEnumerable<IList<Position>> ReturnNullablePaths { get; }
    }
}
