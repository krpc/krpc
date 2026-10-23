using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when an error occurs connecting to a server, or when the connection fails or ends.
    /// </summary>
    [Serializable]
    public class ConnectionException : Exception
    {
        /// <summary>
        /// Construct an RPCException with no message.
        /// </summary>
        public ConnectionException ()
        {
        }

        /// <summary>
        /// Construct an RPCException with the given message.
        /// </summary>
        public ConnectionException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct an RPCException with the given message and inner exception.
        /// </summary>
        public ConnectionException (string message, Exception inner) : base (message, inner)
        {
        }

        /// <summary>
        /// The status of the server's response when it refused the connection, such as
        /// <c>Denied</c>, or null.
        /// </summary>
        public Schema.KRPC.ConnectionResponse.Types.Status? Status { get; internal set; }

        /// <summary>
        /// The reason the server gave when it ended the connection, such as
        /// <c>ServerStopped</c>, or null.
        /// </summary>
        public Schema.KRPC.Disconnect.Types.Reason? Reason { get; internal set; }

#if !NET
        /// <summary>
        /// Construct an RPCException with the given serialization info and streaming context.
        /// </summary>
        protected ConnectionException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
#endif
    }
}
