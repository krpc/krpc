using System;

namespace KRPC.Server
{
    public class ServerException : Exception
    {
        public ServerException (string message) :
            base (message)
        {
        }
    }
}

