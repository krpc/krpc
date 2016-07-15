using System;
using System.Collections.Generic;
using KRPC.Server.HTTP;
using KRPC.Service.Messages;

namespace KRPC.Server.WebSockets
{
    sealed class StreamServer : Message.StreamServer
    {
        readonly IDictionary<IClient<byte,byte>,string> clientKeys = new Dictionary<IClient<byte, byte>, string> ();

        public StreamServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// When a client requests a connection, process the websockets HTTP request
        /// </summary>
        protected override IClient<NoMessage,StreamMessage> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var request = ConnectionRequest.ReadRequest (args);
            if (args.Request.ShouldDeny)
                return null;
            var guid = GetGuid (request);
            if (guid == Guid.Empty) {
                args.Client.Stream.Write (HTTPResponse.BadRequest.ToBytes ());
                args.Request.Deny ();
                return null;
            }
            clientKeys [args.Client] = request.Headers ["Sec-WebSocket-Key"];
            return new StreamClient (guid, args.Client);
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
        /// Get the client guid from a connection request
        /// Returns an empty guid on failure
        /// </summary>
        public static Guid GetGuid (HTTPRequest request)
        {
            //TODO: make this id extraction more robust
            var query = request.URI.Query;
            if (!query.StartsWith ("?id="))
                return Guid.Empty;
            try {
                var bytes = Convert.FromBase64String (query.Substring ("?id=".Length));
                return bytes.Length == 16 ? new Guid (bytes) : Guid.Empty;
            } catch (FormatException) {
                return Guid.Empty;
            }
        }
    }
}
