using System;
using System.Collections.Generic;
using KRPC.Utils;
using KRPC.Service.Messages;
using System.Linq;

namespace KRPC.Server.WebSockets
{
    public sealed class StreamServer : Message.StreamServer
    {
        readonly IDictionary<IClient<byte,byte>,string> clientKeys = new Dictionary<IClient<byte, byte>, string> ();

        public StreamServer (IServer<byte,byte> server) : base (server)
        {
        }

        public override string Address {
            get { return "ws://" + base.Address; }
        }

        /// <summary>
        /// When a client requests a connection, process the websockets HTTP request
        /// </summary>
        protected override IClient<NoMessage,StreamUpdate> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var request = ConnectionRequest.ReadRequest (args);
            if (args.Request.ShouldDeny)
                return null;
            var guid = GetGuid (request);
            if (guid == Guid.Empty) {
                args.Client.Stream.Write (HTTP.Response.CreateBadRequest ().ToBytes ());
                args.Request.Deny ();
                return null;
            }
            clientKeys [args.Client] = request.Headers ["sec-websocket-key"].Single ();
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
        public static Guid GetGuid (HTTP.Request request)
        {
            // TODO: make this id extraction more robust
            var query = request.URI.Query;
            if (!query.StartsWith ("?id=", StringComparison.CurrentCulture)) {
                Logger.WriteLine ("Invalid WebSockets URI: id is not set in query string", Logger.Severity.Error);
                return Guid.Empty;
            }
            var id = query.Substring ("?id=".Length);
            try {
                var bytes = Convert.FromBase64String (id);
                var length = bytes.Length;
                if (length != 16) {
                    Logger.WriteLine ("Invalid WebSockets URI: id is not 16 bytes, got " + length + " bytes: " + id, Logger.Severity.Error);
                    return Guid.Empty;
                }
                return new Guid (bytes);
            } catch (FormatException) {
                Logger.WriteLine ("Invalid WebSockets URI: id is not a valid 16 byte Guid", Logger.Severity.Error);
                return Guid.Empty;
            }
        }
    }
}
