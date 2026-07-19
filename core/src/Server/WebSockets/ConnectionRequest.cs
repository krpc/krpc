using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // Seconds to wait for the rest of a request that arrived split across reads before rejecting it.
        const double TIMEOUT = 3;
        static readonly SHA1 sha1 = SHA1.Create ();

        static readonly IDictionary<IClient<byte,byte>, DynamicBuffer> readBuffers =
            new Dictionary<IClient<byte,byte>, DynamicBuffer> ();
        static readonly IDictionary<IClient<byte,byte>, Stopwatch> readTimers =
            new Dictionary<IClient<byte,byte>, Stopwatch> ();

        /// <summary>
        /// Read a websockets connection request. If the request is invalid, writes the appropriate
        /// HTTP response and denies the connection attempt. Returns null without denying the request
        /// if it has not been fully received yet, in which case the connection attempt should be
        /// retried later.
        /// </summary>
        public static Request ReadRequest (ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            var client = args.Client;
            var stream = client.Stream;

            // The request may arrive split across several reads. Accumulate it until the blank line
            // terminating the HTTP headers has been received.
            DynamicBuffer buffer;
            if (!readBuffers.TryGetValue (client, out buffer)) {
                buffer = new DynamicBuffer ();
                readBuffers [client] = buffer;
            }
            if (stream.DataAvailable) {
                var chunk = new byte [BUFFER_SIZE];
                var count = stream.Read (chunk, 0);
                buffer.Append (chunk, 0, count);
            }

            if (!EndOfHeaders (buffer)) {
                // Not all of the request has arrived. Wait for more data, up to the timeout.
                Stopwatch timer;
                if (!readTimers.TryGetValue (client, out timer)) {
                    timer = Stopwatch.StartNew ();
                    readTimers [client] = timer;
                }
                if (timer.ElapsedSeconds () > TIMEOUT) {
                    Reset (client);
                    Logger.WriteLine (
                        "WebSockets connection request not received after waiting " + TIMEOUT + " seconds",
                        Logger.Severity.Error);
                    args.Request.Deny ();
                    stream.Write (Response.CreateBadRequest ().ToBytes ());
                    return null;
                }
                // Leave the connection attempt pending; it will be retried.
                return null;
            }

            Reset (client);
            try {
                var request = Request.FromBytes (buffer.GetBuffer (), 0, buffer.Length);
                CheckValid (request);
                Logger.WriteLine ("WebSockets: received valid connection request", Logger.Severity.Debug);
                return request;
            } catch (HandshakeException e) {
                Logger.WriteLine ("WebSockets handshake failed: " + e.Response, Logger.Severity.Error);
                args.Request.Deny ();
                stream.Write (e.Response.ToBytes ());
                return null;
            } catch (MalformedRequestException e) {
                Logger.WriteLine ("Malformed WebSockets connection request: " + e.Message, Logger.Severity.Error);
                args.Request.Deny ();
                stream.Write (Response.CreateBadRequest ().ToBytes ());
                return null;
            }
        }

        static void Reset (IClient<byte,byte> client)
        {
            readBuffers.Remove (client);
            readTimers.Remove (client);
        }

        /// <summary>
        /// Returns true once the buffer contains the blank line (\r\n\r\n) that terminates the
        /// HTTP request headers.
        /// </summary>
        static bool EndOfHeaders (DynamicBuffer buffer)
        {
            var data = buffer.GetBuffer ();
            var length = buffer.Length;
            for (var i = 0; i + 3 < length; i++) {
                if (data [i] == (byte)'\r' && data [i + 1] == (byte)'\n' &&
                    data [i + 2] == (byte)'\r' && data [i + 3] == (byte)'\n')
                    return true;
            }
            return false;
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
            if (!request.Headers.ContainsKey ("upgrade") || request.Headers ["upgrade"].SingleOrDefault ().ToLowerInvariant () != "websocket")
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
