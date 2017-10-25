using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server
{
    /// <summary>
    /// The protocol of a server.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "DoNotPrefixValuesWithEnumNameRule")]
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
