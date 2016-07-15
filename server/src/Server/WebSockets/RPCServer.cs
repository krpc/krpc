using System;
using System.Collections.Generic;
using KRPC.Server.HTTP;
using KRPC.Service.Messages;

namespace KRPC.Server.WebSockets
{
    sealed class RPCServer : Message.RPCServer
    {
        readonly bool echo;
        readonly IDictionary<IClient<byte,byte>,string> clientKeys = new Dictionary<IClient<byte, byte>, string> ();

        /// <summary>
        /// Create a websockets RPC server.
        /// If echo is true, the server will simply echo messages back to the client.
        /// This is used for running the Autobahn test suite.
        /// </summary>
        public RPCServer (IServer<byte,byte> server, bool echo = false) : base (server)
        {
            this.echo = echo;
        }

        /// <summary>
        /// When a client requests a connection, process the websockets HTTP request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var request = ConnectionRequest.ReadRequest (args);
            if (args.Request.ShouldDeny)
                return null;
            var clientName = GetClientName (request);
            clientKeys [args.Client] = request.Headers ["Sec-WebSocket-Key"];
            return new RPCClient (clientName, args.Client, echo);
        }

        /// <summary>
        /// Send an upgrade response to the client on successful connection.
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow)
                args.Client.Stream.Write (ConnectionRequest.WriteResponse (clientKeys [args.Client]));
            if (args.Request.ShouldDeny && clientKeys.ContainsKey (args.Client))
                clientKeys.Remove (args.Client);
        }

        /// <summary>
        /// Get the client name from a connection request
        /// </summary>
        public static string GetClientName (HTTPRequest request)
        {
            string name = "";
            var query = request.URI.Query;
            //TODO: make this name extraction more robust
            if (query.StartsWith ("?name="))
                name = query.Substring ("?name=".Length);
            else if (request.Headers.ContainsKey ("Origin"))
                name = request.Headers ["Origin"];
            return Uri.UnescapeDataString (name);
        }
    }
}
