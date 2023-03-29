using System;

namespace KRPC.Server
{
    /// <summary>
    /// The protocol of a server.
    /// </summary>
    [Serializable]
    public enum Protocol
    {
        /// <summary>
        /// A server using Protocol Buffer messages over a TCP connection.
        /// </summary>
        ProtocolBuffersOverTCP,
        /// <summary>
        /// A server using Protocol Buffer messages over a WebSockets server.
        /// </summary>
        ProtocolBuffersOverWebsockets,
        /// <summary>
        /// A server using Protocol Buffer messages over SerialIO.
        /// </summary>
        ProtocolBuffersOverSerialIO
    }
}
