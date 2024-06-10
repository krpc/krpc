using System;
using KRPC.Server.HTTP;

namespace KRPC.Server.WebSockets
{
    sealed class HandshakeException : ServerException
    {
        public Response Response { get; private set; }

        public HandshakeException (Response response)
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
