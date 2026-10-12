using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the KRPC server
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Invoke a remote procedure call
        /// </summary>
        global::KRPC.Schema.KRPC.ProcedureResult Invoke (string service, string procedure, IList<ByteString> arguments = null);

        /// <summary>
        /// Invoke a remote procedure call that returns a value of the given type.
        /// </summary>
        /// <remarks>
        /// Called by generated service stubs. The type is used to note the ids of the streams
        /// and events the procedure returns.
        /// </remarks>
        global::KRPC.Schema.KRPC.ProcedureResult Invoke (string service, string procedure, IList<ByteString> arguments, Type returnType);

        /// <summary>
        /// Add an exception type
        /// </summary>
        void AddExceptionType (string service, string name, Type exnType);
    }
}
