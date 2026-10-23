namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// Server over a serial port. The port carries one client's session at a time.
    /// </summary>
    public sealed class Server : Message.Server
    {
        /// <summary>
        /// Construct a server from a byte server
        /// </summary>
        public Server (IServer<byte,byte> innerServer) : base(innerServer)
        {
        }

        internal override Message.Connection CreateConnection (IClient<byte,byte> client)
        {
            return new Connection (this, client);
        }
    }
}
