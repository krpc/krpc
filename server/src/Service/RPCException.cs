using System;

namespace KRPC.Service
{
    sealed class RPCException : Exception
    {
        public RPCException ()
        {
        }

        public RPCException (string message)
        : base(message)
        {
        }

        public RPCException (Exception innerException)
        : base(string.Empty, innerException)
        {
        }

        public RPCException (string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}
