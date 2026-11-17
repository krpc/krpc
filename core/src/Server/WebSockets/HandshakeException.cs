using System;

namespace KRPC.Server.WebSockets
{
    sealed class HandshakeException : ServerException
    {
        public HttpResponse Response { get; private set; }

        public HandshakeException (HttpResponse response)
        {
            Response = response;
        }

        public HandshakeException ()
        {
        }

        public HandshakeException (string message) : base (message)
        {
        }

        public HandshakeException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
