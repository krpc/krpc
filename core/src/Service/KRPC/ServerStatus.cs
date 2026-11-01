using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The status of the server.
    /// </summary>
    /// <remarks>
    /// See <see cref="KRPC.GetStatus"/>.
    /// </remarks>
    [KRPCStruct (Service = "KRPC")]
    public struct ServerStatus
    {
        /// <summary>
        /// The version of the server, or <c>unknown</c>.
        /// </summary>
        [KRPCProperty]
        public string Version { get; set; }

        /// <summary>
        /// The protocol version of the connection.
        /// </summary>
        [KRPCProperty]
        public uint ProtocolVersion { get; set; }

        /// <summary>
        /// The settings of the server.
        /// </summary>
        [KRPCProperty]
        public ServerSettings Settings { get; set; }

        /// <summary>
        /// The performance statistics of the server.
        /// </summary>
        [KRPCProperty]
        public ServerStatistics Statistics { get; set; }
    }
}
