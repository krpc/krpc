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
        public Server (IServer<IByteStream> server, AllowedOrigins origins = null) : base (server)
        {
            AllowedOrigins = origins ?? new AllowedOrigins ("localhost");
        }

        /// <summary>
        /// The origins whose web pages may connect. A client that is not a web page sends no
        /// origin, and may always connect.
        /// </summary>
        public AllowedOrigins AllowedOrigins { get; private set; }

        /// <summary>
        /// Server address.
        /// </summary>
        public override string Address {
            get { return "ws://" + base.Address; }
        }

        internal override Message.Connection CreateConnection (IClient<IByteStream> client)
        {
            return new Connection (this, client);
        }
    }
}
