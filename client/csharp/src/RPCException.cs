using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
    /// <summary>
    /// Thrown when a error occurs executing a remote procedure.
    /// </summary>
    [Serializable ()]
    public class RPCException : Exception
    {
        public RPCException () : base ()
        {
        }

        public RPCException (string message) : base (message)
        {
        }

        public RPCException (string message, System.Exception inner) : base (message, inner)
        {
        }

        protected RPCException (SerializationInfo info, StreamingContext context)
        {
        }
    }
}
