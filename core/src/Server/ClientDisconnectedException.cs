using System;

namespace KRPC.Server
{
    public sealed class ClientDisconnectedException: ServerException
    {
        public ClientDisconnectedException ()
        {
        }

        public ClientDisconnectedException (string message) : base (message)
        {
        }

        public ClientDisconnectedException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
