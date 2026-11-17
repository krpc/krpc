using System;

namespace KRPC.Server.WebSockets
{
    sealed class MalformedHttpRequestException: ServerException
    {
        public MalformedHttpRequestException ()
        {
        }

        public MalformedHttpRequestException (string message) : base (message)
        {
        }

        public MalformedHttpRequestException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
