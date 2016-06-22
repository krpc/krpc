using System;

namespace KRPC.Server.Message
{
    sealed class NoRequestException : Exception
    {
        public NoRequestException ()
        {
        }

        public NoRequestException (string message) : base (message)
        {
        }

        public NoRequestException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
