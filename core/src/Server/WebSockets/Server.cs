using KRPC.Utils;

namespace KRPC.Server.WebSockets
{
    /// <summary>
    /// Server over websockets, with one message per binary websockets message.
    /// </summary>
    public sealed class Server : Message.Server
    {
        readonly bool shouldEcho;

        /// <summary>
        /// Create a websockets server.
        /// If echo is true, the server will simply echo messages back to the client.
        /// This is used for running the Autobahn test suite.
        /// </summary>
        public Server (IServer<byte,byte> server, bool echo = false) : base (server)
        {
            shouldEcho = echo;
            if (echo)
                Logger.WriteLine ("WebSockets server running in echo mode", Logger.Severity.Warning);
        }

        /// <summary>
        /// Server address.
        /// </summary>
        public override string Address {
            get { return "ws://" + base.Address; }
        }

        internal override Message.Connection CreateConnection (IClient<byte,byte> client)
        {
            return new Connection (this, client, shouldEcho);
        }
    }
}
