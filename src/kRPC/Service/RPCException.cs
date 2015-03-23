using System;

namespace KRPC.Service
{
    class RPCException : Exception
    {
        public RPCException (string message) :
            base (message)
        {
        }
    }
}

