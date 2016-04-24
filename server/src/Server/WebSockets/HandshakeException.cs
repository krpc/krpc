using System;
using KRPC.Server.HTTP;

namespace KRPC
{
    class HandshakeException : Exception
    {
        public HTTPResponse Response { get; private set; }

        public HandshakeException (HTTPResponse response)
        {
            Response = response;
        }

        public HandshakeException (HTTPResponse response, string message)
        {
            Response = response;
            response.Body = message;
        }
    }
}
