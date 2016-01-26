using System;
using System.Runtime.Serialization;

namespace KRPC.Client
{
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
