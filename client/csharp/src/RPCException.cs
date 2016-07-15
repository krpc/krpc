using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a error occurs executing a remote procedure.
    /// </summary>
    [Serializable]
    public class RPCException : Exception
    {
        /// <summary>
        /// Construct an RPCException with no message.
        /// </summary>
        public RPCException ()
        {
        }

        /// <summary>
        /// Construct an RPCException with the given message.
        /// </summary>
        public RPCException (string message) : base (message)
        {
        }

        /// <summary>
        /// Construct an RPCException with the given message and inner exception.
        /// </summary>
        public RPCException (string message, Exception inner) : base (message, inner)
        {
        }

        /// <summary>
        /// Construct an RPCException with the given serialization info and streaming context.
        /// </summary>
        protected RPCException (SerializationInfo info, StreamingContext context) : base (info, context)
        {
        }
    }
}
