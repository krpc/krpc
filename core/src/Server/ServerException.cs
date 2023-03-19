using System;

namespace KRPC.Server
{
    class ServerException : Exception
    {
        public ServerException ()
        {
        }

        public ServerException (string message) : base (message)
        {
        }

        public ServerException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
