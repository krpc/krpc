using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.WebSockets
{
    /// <summary>
    /// RPC server for receiving requests and sending responses over an underlying message server.
    /// </summary>
    public sealed class RPCServer : Message.RPCServer
    {
        readonly bool shouldEcho;
        readonly IDictionary<IClient<byte,byte>,byte[]> clientResponses = new Dictionary<IClient<byte, byte>, byte[]> ();

        /// <summary>
        /// Create a websockets RPC server.
        /// If echo is true, the server will simply echo messages back to the client.
        /// This is used for running the Autobahn test suite.
        /// </summary>
        public RPCServer (IServer<byte,byte> server, bool echo = false) : base (server)
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

        /// <summary>
        /// When a client requests a connection, process the websockets HTTP request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var address = args.Client.Address;
            Logger.WriteLine ("WebSockets: client requesting connection (" + address + ")", Logger.Severity.Debug);
            var request = ConnectionRequest.ReadRequest (args);
            if (args.Request.ShouldDeny) {
                Logger.WriteLine ("WebSockets: client connection denied (" + address + ")", Logger.Severity.Error);
                return null;
            }
            if (request == null)
                // The request has not been fully received yet; retry on a later update.
                return null;
            var clientName = GetClientName (request);
            var subprotocol = ConnectionRequest.GetSubprotocol (request);
            var protocolVersion = ConnectionRequest.GetProtocolVersion (subprotocol);
            clientResponses [args.Client] = ConnectionRequest.WriteResponse (
                request.Headers ["sec-websocket-key"].Single (), subprotocol);
            return new RPCClient (clientName, protocolVersion, args.Client, shouldEcho);
        }

        /// <summary>
        /// Send an upgrade response to the client on successful connection.
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow) {
                args.Client.Stream.Write (clientResponses [args.Client]);
                Logger.WriteLine ("WebSockets: client connection accepted (" + args.Client.Address + ")");
            }
            if (args.Request.ShouldDeny && clientResponses.ContainsKey (args.Client))
                clientResponses.Remove (args.Client);
        }

        /// <summary>
        /// Get the client name from a connection request
        /// </summary>
        public static string GetClientName (HTTP.Request request)
        {
            var name = request.QueryParameter ("name");
            if (name == null && request.Headers.ContainsKey ("origin"))
                name = request.Headers ["origin"].First ();
            return name ?? string.Empty;
        }
    }
}
