using System;

namespace KRPC.Server
{
    /// <summary>
    /// Client disconnected from the server
    /// </summary>
    public sealed class ClientDisconnectedException: ServerException
    {
        #pragma warning disable 1591
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
