using System;

namespace KRPC.Server
{
    class ServerException : Exception
    {
        public ServerException (string message) :
            base (message)
        {
        }
    }
}

