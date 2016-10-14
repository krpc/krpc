using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a error occurs connecting to a server.
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
        /// Construct an RPCException with the given serialization info and streaming context.
        /// </summary>
        protected ConnectionException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
    }
}
