using System;

namespace KRPC.Service
{
    public class RPCException : Exception
    {
        public RPCException (string message):
            base(message)
        {
        }
    }
}

