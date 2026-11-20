using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using KRPC.Utils;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// An HTTP/1.1 connection. Parses the requests a client sends, and sends the responses in
    /// the order of the requests, as each one completes.
    /// </summary>
    sealed class Connection
    {
        internal const int MaxHeaderBytes = 16 * 1024;
        internal const int MaxBodyBytes = 16 * 1024 * 1024;

        readonly IClient<IByteStream> client;
        byte[] buffer = new byte [4096];
        int length;
        readonly Queue<Slot> slots = new Queue<Slot> ();
        // Set once a response closes the connection, after which further requests are dropped
        bool ended;
        // Whether the current request has been sent its 100 Continue
        bool continued;

        /// <summary>
        /// The place of a response in the order of the responses.
        /// </summary>
        public sealed class Slot
        {
            internal byte[] Response;
            internal bool Close;
            // The origin of a cross-origin request the server allows, which its response names
            internal string Origin;
        }

        public Connection (IClient<IByteStream> byteClient)
        {
            client = byteClient;
        }

        public IClient<IByteStream> Client {
            get { return client; }
        }

        /// <summary>
        /// The number of requests whose responses have not been sent.
        /// </summary>
        public int Waiting {
            get { return slots.Count; }
        }

        /// <summary>
        /// The next complete request the client has sent, or null if there is none yet.
        /// Throws a <see cref="RequestException"/> for a malformed request.
        /// </summary>
        public HttpRequest Next ()
        {
            // The socket closes only once its received bytes are read, so we discard what
            // arrives after the last request
            if (ended) {
                Discard ();
                return null;
            }
            if (!client.Connected)
                return null;
            Receive ();
            var end = HeaderEnd ();
            if (end > MaxHeaderBytes || (end < 0 && length > MaxHeaderBytes))
                throw new RequestException (431, "The request header is larger than " + MaxHeaderBytes + " bytes");
            if (end < 0)
                return null;
            var lines = Encoding.ASCII.GetString (buffer, 0, end).Split (new [] { "\r\n" }, StringSplitOptions.None);
            var requestLine = lines [0].Split (' ');
            if (requestLine.Length != 3 || requestLine [0].Length == 0 || !requestLine [1].StartsWith ("/", StringComparison.Ordinal))
                throw new RequestException (400, "Malformed request line");
            var version = requestLine [2];
            if (version != "HTTP/1.1" && version != "HTTP/1.0")
                throw new RequestException (505, "Unsupported version " + version);
            var headers = new Dictionary<string, string> ();
            for (int i = 1; i < lines.Length; i++) {
                var colon = lines [i].IndexOf (':');
                // A bare CR or LF would end the line early in a response header that echoes the value
                if (colon <= 0 || lines [i].IndexOfAny (lineBreaks) >= 0)
                    throw new RequestException (400, "Malformed header field");
                var name = lines [i].Substring (0, colon).Trim ().ToLowerInvariant ();
                var value = lines [i].Substring (colon + 1).Trim ();
                string existing;
                headers [name] = headers.TryGetValue (name, out existing) ? existing + ", " + value : value;
            }
            if (headers.ContainsKey ("transfer-encoding"))
                throw new RequestException (501, "Transfer-Encoding is not supported, send Content-Length");
            var bodyLength = 0;
            string contentLength;
            if (headers.TryGetValue ("content-length", out contentLength) &&
                !int.TryParse (contentLength, NumberStyles.None, CultureInfo.InvariantCulture, out bodyLength))
                throw new RequestException (400, "Malformed Content-Length");
            if (bodyLength > MaxBodyBytes)
                throw new RequestException (413, "The request body is larger than " + MaxBodyBytes + " bytes");
            var bodyStart = end + 4;
            if (length < bodyStart + bodyLength) {
                string expect;
                if (!continued && slots.Count == 0 && headers.TryGetValue ("expect", out expect) &&
                    expect.Equals ("100-continue", StringComparison.OrdinalIgnoreCase)) {
                    continued = true;
                    Send (continueResponse);
                }
                return null;
            }
            continued = false;
            var body = new byte [bodyLength];
            Array.Copy (buffer, bodyStart, body, 0, bodyLength);
            Consume (bodyStart + bodyLength);

            string connection;
            headers.TryGetValue ("connection", out connection);
            connection = (connection ?? string.Empty).ToLowerInvariant ();
            var keepAlive = version == "HTTP/1.1" ? !connection.Contains ("close") : connection.Contains ("keep-alive");
            return new HttpRequest (requestLine [0], requestLine [1], headers, body, keepAlive);
        }

        static readonly char[] lineBreaks = { '\r', '\n' };
        static readonly byte[] continueResponse = Encoding.ASCII.GetBytes ("HTTP/1.1 100 Continue\r\n\r\n");

        void Discard ()
        {
            var stream = client.Stream;
            while (stream.DataAvailable && stream.Read (buffer, 0, buffer.Length) > 0) {
            }
            length = 0;
        }

        void Send (byte[] data)
        {
            try {
                client.Stream.Write (data);
            } catch (ServerException e) {
                Logger.WriteLine ("HTTP: failed to send to " + client.Address + ": " + e.Message, Logger.Severity.Debug);
                slots.Clear ();
                ended = true;
            }
        }

        void Receive ()
        {
            var stream = client.Stream;
            while (stream.DataAvailable) {
                if (length == buffer.Length) {
                    // Room for the largest request, and no more
                    if (buffer.Length >= MaxHeaderBytes + MaxBodyBytes + 4)
                        return;
                    Array.Resize (ref buffer, buffer.Length * 2);
                }
                var read = stream.Read (buffer, length, buffer.Length - length);
                if (read == 0)
                    return;
                length += read;
            }
        }

        int HeaderEnd ()
        {
            for (int i = 0; i + 3 < length; i++)
                if (buffer [i] == '\r' && buffer [i + 1] == '\n' && buffer [i + 2] == '\r' && buffer [i + 3] == '\n')
                    return i;
            return -1;
        }

        void Consume (int count)
        {
            Array.Copy (buffer, count, buffer, 0, length - count);
            length -= count;
        }

        /// <summary>
        /// Take the next place in the order of the responses. With <paramref name="close"/>,
        /// the connection closes after the response, and takes no further requests.
        /// </summary>
        public Slot Reserve (bool close)
        {
            var slot = new Slot { Close = close };
            slots.Enqueue (slot);
            if (close)
                ended = true;
            return slot;
        }

        /// <summary>
        /// Set the response of a slot, and send each response that has its turn.
        /// </summary>
        public void Complete (Slot slot, byte[] response)
        {
            slot.Response = response;
            try {
                while (slots.Count > 0 && slots.Peek ().Response != null) {
                    var next = slots.Dequeue ();
                    client.Stream.Write (next.Response);
                    if (next.Close) {
                        var stream = client.Stream as IHalfClosableStream;
                        if (stream != null)
                            stream.HalfClose ();
                        else
                            client.Close ();
                        slots.Clear ();
                    }
                }
            } catch (ServerException e) {
                Logger.WriteLine ("HTTP: failed to send a response to " + client.Address + ": " + e.Message, Logger.Severity.Debug);
                slots.Clear ();
                ended = true;
            }
        }
    }
}
