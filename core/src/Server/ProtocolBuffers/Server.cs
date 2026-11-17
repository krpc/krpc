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
        public Server (IServer<IByteStream> server) : base (server)
        {
        }

        internal override Message.Connection CreateConnection (IClient<IByteStream> client)
        {
            return new Connection (this, client);
        }
    }
}
