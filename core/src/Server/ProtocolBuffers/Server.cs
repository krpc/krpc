namespace KRPC.Server.ProtocolBuffers
{
    /// <summary>
    /// Server, for messages framed by a varint length prefix.
    /// </summary>
    public sealed class Server : Message.Server
    {
        /// <summary>
        /// Construct a server from a byte server.
        /// </summary>
        public Server (IServer<byte,byte> server) : base (server)
        {
        }

        internal override Message.Connection CreateConnection (IClient<byte,byte> client)
        {
            return new Connection (this, client);
        }
    }
}
