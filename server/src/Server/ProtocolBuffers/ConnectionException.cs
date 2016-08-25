using System;
using KRPC.Server;
using KRPC.Schema.KRPC;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class ConnectionException: ServerException
    {
        public ConnectionException ()
        {
        }

        public ConnectionException (string message) : base (message)
        {
        }

        public ConnectionException (string message, Exception innerException) : base (message, innerException)
        {
        }

        public ConnectionException (ConnectionResponse.Types.Status status, string message) : base (message)
        {
            Status = status;
        }

        public ConnectionResponse.Types.Status Status { get; private set; }
    }
}
