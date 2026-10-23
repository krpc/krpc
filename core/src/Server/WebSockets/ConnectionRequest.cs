using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KRPC.Server.HTTP;

namespace KRPC.Server.WebSockets
{
    static class ConnectionRequest
    {
        const string WEB_SOCKETS_KEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        static readonly SHA1 sha1 = SHA1.Create ();

        /// <summary>
        /// The length of the HTTP request at the start of the data, up to and including the blank
        /// line (\r\n\r\n) that terminates its headers, or zero if it has not all arrived.
        /// </summary>
        public static int Length (byte[] data, int offset, int length)
        {
            for (var i = offset; i + 3 < offset + length; i++) {
                if (data [i] == (byte)'\r' && data [i + 1] == (byte)'\n' &&
                    data [i + 2] == (byte)'\r' && data [i + 3] == (byte)'\n')
                    return i + 4 - offset;
            }
            return 0;
        }

        /// <summary>
        /// Parse and check a websockets connection request. Throws a HandshakeException holding
        /// the response to send if the request is invalid.
        /// </summary>
        public static Request Read (byte[] data, int offset, int length)
        {
            Request request;
            try {
                request = Request.FromBytes (data, offset, length);
            } catch (MalformedRequestException) {
                throw new HandshakeException (Response.CreateBadRequest ());
            }
            CheckValid (request);
            return request;
        }

        /// <summary>
        /// The response accepting a connection.
        /// </summary>
        public static byte[] WriteResponse (string key)
        {
            var returnKey = Convert.ToBase64String (sha1.ComputeHash (Encoding.ASCII.GetBytes (key + WEB_SOCKETS_KEY)));
            var response = new Response (101, "Switching Protocols");
            response.AddHeaderField ("Upgrade", "websocket");
            response.AddHeaderField ("Connection", "Upgrade");
            response.AddHeaderField ("Sec-WebSocket-Accept", returnKey);
            return response.ToBytes ();
        }

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
            if (!request.Headers.ContainsKey ("upgrade") || (SingleValue (request, "upgrade") ?? string.Empty).ToLowerInvariant () != "websocket")
                throw new HandshakeException (Response.CreateBadRequest ("Upgrade field not set to websocket."));

            // Check connection field
            if (!request.Headers.ContainsKey ("connection") || !request.Headers ["connection"].Contains ("upgrade", StringComparer.CurrentCultureIgnoreCase))
                throw new HandshakeException (Response.CreateBadRequest ("Connection field not set to Upgrade."));

            // Check key field
            if (!request.Headers.ContainsKey ("sec-websocket-key"))
                throw new HandshakeException (Response.CreateBadRequest ("Sec-WebSocket-Key field not set."));
            try {
                var key = Convert.FromBase64String (SingleValue (request, "sec-websocket-key") ?? string.Empty);
                if (key.Length != 16)
                    throw new HandshakeException (Response.CreateBadRequest ("Failed to decode Sec-WebSocket-Key\nExpected 16 bytes, got " + key.Length + " bytes."));
            } catch (FormatException) {
                throw new HandshakeException (Response.CreateBadRequest ("Failed to decode Sec-WebSocket-Key\nNot a valid base64 string."));
            }

            // Check version field
            if (!request.Headers.ContainsKey ("sec-websocket-version") || SingleValue (request, "sec-websocket-version") != "13") {
                var response = Response.CreateUpgradeRequired ();
                response.AddHeaderField ("Sec-WebSocket-Version", "13");
                throw new HandshakeException (response);
            }

            // Note: Sec-WebSocket-Protocol and Sec-WebSocket-Extensions fields are ignored
        }

        /// <summary>
        /// The value of a header field holding one value, or null if it holds several.
        /// </summary>
        static string SingleValue (Request request, string key)
        {
            var values = request.Headers [key];
            return values.Count == 1 ? values [0] : null;
        }
    }
}
