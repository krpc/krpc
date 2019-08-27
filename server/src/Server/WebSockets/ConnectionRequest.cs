using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KRPC.Server.HTTP;
using KRPC.Utils;

namespace KRPC.Server.WebSockets
{
    static class ConnectionRequest
    {
        const string WEB_SOCKETS_KEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        const int BUFFER_SIZE = 4096;
        static readonly SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider ();

        /// <summary>
        /// Read a websockets connection request. If the request is invalid,
        /// writes the approprate HTTP response and denies the connection attempt.
        /// </summary>
        public static Request ReadRequest (ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var stream = args.Client.Stream;
            try {
                var buffer = new byte [BUFFER_SIZE];
                var count = stream.Read (buffer, 0);
                var request = Request.FromBytes (buffer, 0, count);
                CheckValid (request);
                Logger.WriteLine ("WebSockets: received valid connection request", Logger.Severity.Debug);
                return request;
            } catch (HandshakeException e) {
                Logger.WriteLine ("WebSockets handshake failed: " + e.Response, Logger.Severity.Error);
                args.Request.Deny ();
                stream.Write (e.Response.ToBytes ());
                return null;
            } catch (MalformedRequestException e) {
                // TODO: wait for timeout seconds to see if the request was truncated
                Logger.WriteLine ("Malformed WebSockets connection request: " + e.Message, Logger.Severity.Error);
                args.Request.Deny ();
                stream.Write (Response.CreateBadRequest ().ToBytes ());
                return null;
            }
        }

        public static byte[] WriteResponse (string key)
        {
            var returnKey = Convert.ToBase64String (sha1.ComputeHash (Encoding.ASCII.GetBytes (key + WEB_SOCKETS_KEY)));
            var response = new Response (101, "Switching Protocols");
            response.AddHeaderField ("Upgrade", "websocket");
            response.AddHeaderField ("Connection", "Upgrade");
            response.AddHeaderField ("Sec-WebSocket-Accept", returnKey);
            return response.ToBytes ();
        }

        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        static void CheckValid (Request request)
        {
            // Check request line
            if (request.Protocol != "http/1.1")
                throw new HandshakeException (Response.CreateHTTPVersionNotSupported ());
            if (request.URI.AbsolutePath != "/")
                throw new HandshakeException (Response.CreateNotFound ());
            if (request.Method != "get")
                throw new HandshakeException (Response.CreateMethodNotAllowed ("Expected a GET request."));

            // Check host field
            if (!request.Headers.ContainsKey ("host"))
                throw new HandshakeException (Response.CreateBadRequest ("Host field not set."));

            // Check upgrade field
            if (!request.Headers.ContainsKey ("upgrade") || request.Headers ["upgrade"].SingleOrDefault ().ToLower () != "websocket")
                throw new HandshakeException (Response.CreateBadRequest ("Upgrade field not set to websocket."));

            // Check connection field
            if (!request.Headers.ContainsKey ("connection") || !request.Headers ["connection"].Contains ("upgrade", StringComparer.CurrentCultureIgnoreCase))
                throw new HandshakeException (Response.CreateBadRequest ("Connection field not set to Upgrade."));

            // Check key field
            if (!request.Headers.ContainsKey ("sec-websocket-key"))
                throw new HandshakeException (Response.CreateBadRequest ("Sec-WebSocket-Key field not set."));
            try {
                var key = Convert.FromBase64String (request.Headers ["sec-websocket-key"].SingleOrDefault ());
                if (key.Length != 16)
                    throw new HandshakeException (Response.CreateBadRequest ("Failed to decode Sec-WebSocket-Key\nExpected 16 bytes, got " + key.Length + " bytes."));
            } catch (FormatException) {
                throw new HandshakeException (Response.CreateBadRequest ("Failed to decode Sec-WebSocket-Key\nNot a valid base64 string."));
            }

            // Check version field
            if (!request.Headers.ContainsKey ("sec-websocket-version") || request.Headers ["sec-websocket-version"].SingleOrDefault () != "13") {
                var response = Response.CreateUpgradeRequired ();
                response.AddHeaderField ("Sec-WebSocket-Version", "13");
                throw new HandshakeException (response);
            }

            // Note: Sec-WebSocket-Protocol and Sec-WebSocket-Extensions fields are ignored
        }
    }
}
