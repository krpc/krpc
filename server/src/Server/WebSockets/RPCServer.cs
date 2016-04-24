using System.Security.Cryptography;
using System.Text;
using KRPC.Server.HTTP;
using KRPC.Service.Messages;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Server.WebSockets
{
    sealed class RPCServer : Message.RPCServer
    {
        const string WEB_SOCKETS_KEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        const int BUFFER_SIZE = 4096;
        readonly byte[] buffer = new byte[BUFFER_SIZE];
        readonly SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider ();
        readonly IDictionary<IClient<byte,byte>,string> clientKeys = new Dictionary<IClient<byte, byte>, string> ();

        public RPCServer (IServer<byte,byte> server) : base (server)
        {
        }

        /// <summary>
        /// When a client requests a connection, process the websockets HTTP request
        /// </summary>
        protected override IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionArgs<byte,byte> args)
        {
            try {
                var request = ReadRequest (args.Client.Stream);
                var clientName = "unknown";
                if (request.Headers.ContainsKey ("Origin"))
                    clientName = request.Headers ["Origin"];            
                clientKeys [args.Client] = request.Headers ["Sec-WebSocket-Key"];
                return new RPCClient (clientName, args.Client);
            } catch (HandshakeException e) {
                Logger.WriteLine ("WebSockets handshake exception: " + e.Response, Logger.Severity.Debug);
                args.Request.Deny ();
                clientKeys.Remove (args.Client);
                args.Client.Stream.Write (e.Response.ToBytes ());
                return null;
            } catch (MalformedRequest e) {
                //TODO: wait for timeout seconds to see if the request was truncated
                Logger.WriteLine ("WebSockets malformed request: " + e.Message, Logger.Severity.Debug);
                args.Request.Deny ();
                clientKeys.Remove (args.Client);
                return null;
            }
        }

        /// <summary>
        /// Send an upgrade response to the client on successful connection.
        /// </summary>
        public override void HandleClientRequestingConnection (object sender, ClientRequestingConnectionArgs<byte,byte> args)
        {
            base.HandleClientRequestingConnection (sender, args);
            if (args.Request.ShouldAllow) {
                if (!clientKeys.ContainsKey (args.Client))
                    args.Client.Stream.Write (HTTPResponse.InternalServerError.ToBytes ());
                var clientKey = clientKeys [args.Client];
                var returnKey = System.Convert.ToBase64String (sha1.ComputeHash (Encoding.ASCII.GetBytes (clientKey + WEB_SOCKETS_KEY)));
                var response = new HTTPResponse (101, "Switching Protocols");
                response.AddAttribute ("Upgrade", "websocket");
                response.AddAttribute ("Connection", "Upgrade");
                response.AddAttribute ("Sec-WebSocket-Accept", returnKey);
                args.Client.Stream.Write (response.ToBytes ());
            }
        }

        /// <summary>
        /// Read a websockets connection request. If the request is invalid,
        /// throws a HandshakeException containing the HTTPResponse to send to the client.
        /// </summary>
        HTTPRequest ReadRequest (IStream<byte,byte> stream)
        {
            var count = stream.Read (buffer, 0);
            var request = HTTPRequest.FromBytes (buffer, 0, count);
            if (request.Protocol != "HTTP/1.1")
                throw new HandshakeException (HTTPResponse.HTTPVersionNotSupported);
            if (request.URI != "/")
                throw new HandshakeException (HTTPResponse.NotFound);
            if (request.Method != "GET")
                throw new HandshakeException (HTTPResponse.MethodNotAllowed);

            // Check host field
            if (!request.Headers.ContainsKey ("Host"))
                throw new HandshakeException (HTTPResponse.BadRequest, "Host key not found");

            // Check upgrade field
            if (!request.Headers.ContainsKey ("Upgrade") || request.Headers ["Upgrade"].ToLower () != "websocket")
                throw new HandshakeException (HTTPResponse.BadRequest, "Upgrade field must be set to websocket");
            
            // Check connection field
            if (!request.Headers.ContainsKey ("Connection") || request.Headers ["Connection"].ToLower () != "upgrade")
                throw new HandshakeException (HTTPResponse.BadRequest, "Connection header field must be set to Upgrade");

            // Get key field
            if (!request.Headers.ContainsKey ("Sec-WebSocket-Key"))
                throw new HandshakeException (HTTPResponse.BadRequest, "Sec-WebSocket-Key must be set");
            var key = System.Convert.FromBase64String (request.Headers ["Sec-WebSocket-Key"]);
            if (key.Length != 16)
                throw new HandshakeException (HTTPResponse.BadRequest, "Decoded Sec-WebSocket-Key not 16 bytes in length, got " + key.Length + " bytes");

            // Check version field
            if (!request.Headers.ContainsKey ("Sec-WebSocket-Version") || request.Headers ["Sec-WebSocket-Version"] != "13") {
                var response = HTTPResponse.UpgradeRequired;
                response.AddAttribute ("Sec-WebSocket-Version", "13");
                throw new HandshakeException (response);
            }

            // Note: Sec-WebSocket-Protocol and Sec-WebSocket-Extensions fields are ignored
            return request;
        }
    }
}
