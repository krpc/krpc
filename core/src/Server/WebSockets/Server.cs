namespace KRPC.Server.WebSockets
{
    /// <summary>
    /// Server over websockets, with one message per binary websockets message.
    /// </summary>
    public sealed class Server : Message.Server
    {
        /// <summary>
        /// Create a websockets server.
        /// </summary>
        public Server (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// Server address.
        /// </summary>
        public override string Address {
            get { return "ws://" + base.Address; }
        }

        internal override Message.Connection CreateConnection (IClient<byte,byte> client)
        {
            return new Connection (this, client);
        }
    }
}
