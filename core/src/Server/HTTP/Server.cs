using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.TCP;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// A server that takes calls as HTTP requests with JSON bodies. All of them are calls on
    /// one client, the session, which lasts while the server runs.
    /// </summary>
    public sealed class Server : IMessageServer
    {
        /// <summary>
        /// Raised once the server has started.
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Raised once the server has stopped.
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Never raised. Starting the server approves its session.
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs<IMessageStream>> OnClientRequestingConnection {
            add { }
            remove { }
        }

        /// <summary>
        /// Raised when the session starts, on the first update after the server starts.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<IMessageStream>> OnClientConnected;

        /// <summary>
        /// Raised when the session ends, as the server stops.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<IMessageStream>> OnClientDisconnected;

        sealed class Pending
        {
            public Schema.KRPC.Request Request;
            public Route Route;
            public Connection Connection;
            public Connection.Slot Slot;
            public long Arrived;
        }

        readonly TCPServer tcpServer;
        readonly Dictionary<IClient<IByteStream>, Connection> connections = new Dictionary<IClient<IByteStream>, Connection> ();
        readonly List<Connection> updating = new List<Connection> ();
        Session session;
        bool sessionStarted;
        Definitions definitions;
        ulong nextId = 1;
        readonly Queue<Pending> queued = new Queue<Pending> ();
        readonly Dictionary<ulong, Pending> running = new Dictionary<ulong, Pending> ();

        /// <summary>
        /// Create a server that accepts HTTP connections through a TCP/IP server.
        /// </summary>
        public Server (TCPServer server)
        {
            tcpServer = server;
            tcpServer.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            tcpServer.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            tcpServer.OnClientConnected += (s, e) => connections [e.Client] = new Connection (e.Client);
            tcpServer.OnClientDisconnected += (s, e) => connections.Remove (e.Client);
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            definitions = Definitions.Instance;
            tcpServer.Start ();
            session = new Session (this);
            sessionStarted = false;
        }

        /// <summary>
        /// Stop the server, ending its session.
        /// </summary>
        public void Stop ()
        {
            if (sessionStarted) {
                sessionStarted = false;
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<IMessageStream> (session));
            }
            queued.Clear ();
            running.Clear ();
            tcpServer.Stop ();
            connections.Clear ();
        }

        /// <summary>
        /// Accept connections, and read the requests they have sent.
        /// </summary>
        public void Update ()
        {
            tcpServer.Update ();
            if (!Running)
                return;
            if (!sessionStarted) {
                sessionStarted = true;
                EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<IMessageStream> (session));
            }
            ReadConnections ();
        }

        void ReadConnections ()
        {
            updating.Clear ();
            updating.AddRange (connections.Values);
            foreach (var connection in updating)
                Read (connection);
        }

        /// <summary>
        /// Does nothing. A response is sent as soon as it is written.
        /// </summary>
        public void Flush ()
        {
        }

        /// <summary>
        /// The URL the server listens on.
        /// </summary>
        public string Address {
            get { return "http://" + tcpServer.Address; }
        }

        /// <summary>
        /// Which clients can reach the server.
        /// </summary>
        public string Info {
            get { return tcpServer.Info; }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return tcpServer.Running; }
        }

        /// <summary>
        /// The session, once it has started.
        /// </summary>
        public IEnumerable<IClient<IMessageStream>> Clients {
            get {
                if (sessionStarted)
                    yield return session;
            }
        }

        /// <summary>
        /// The bytes read from every connection.
        /// </summary>
        public ulong BytesRead {
            get { return tcpServer.BytesRead; }
        }

        /// <summary>
        /// The bytes written to every connection.
        /// </summary>
        public ulong BytesWritten {
            get { return tcpServer.BytesWritten; }
        }

        /// <summary>
        /// Reset the byte counts.
        /// </summary>
        public void ClearStats ()
        {
            tcpServer.ClearStats ();
        }

        internal bool IsSession (Session client)
        {
            return ReferenceEquals (client, session) && sessionStarted;
        }

        // The core polls this while it waits for calls, so a request that arrives during an
        // update runs in that update
        internal bool HasRequest {
            get {
                if (queued.Count == 0 && Running)
                    ReadConnections ();
                return queued.Count > 0;
            }
        }

        internal Request TakeRequest ()
        {
            var pending = queued.Dequeue ();
            running [pending.Request.Id] = pending;
            var request = pending.Request.ToMessage ();
            request.Arrived = pending.Arrived;
            return request;
        }

        internal void Respond (Response response)
        {
            Pending pending;
            if (!running.TryGetValue (response.Id, out pending))
                return;
            running.Remove (response.Id);
            int status;
            string body;
            try {
                Render (pending.Route, response.ToProtobufMessage (), out status, out body);
            } catch (System.Exception e) {
                status = 500;
                body = ErrorBody (string.Empty, string.Empty, "Failed to encode the result: " + e.Message, string.Empty);
            }
            pending.Connection.Complete (pending.Slot, BuildResponse (status, body, pending.Slot.Close, null));
        }

        // Requests a connection may have waiting for their responses. A pipelining client past
        // this has its later requests left in the socket, unread.
        const int MaxWaiting = 64;

        void Read (Connection connection)
        {
            while (connection.Waiting < MaxWaiting) {
                HttpRequest request;
                try {
                    request = connection.Next ();
                } catch (RequestException e) {
                    connection.Complete (connection.Reserve (true), BuildResponse (e, true));
                    return;
                }
                if (request == null)
                    return;
                var slot = connection.Reserve (!request.KeepAlive);
                try {
                    Handle (connection, slot, request);
                } catch (RequestException e) {
                    connection.Complete (slot, BuildResponse (e, slot.Close));
                } catch (System.Exception e) {
                    // An unexpected exception fails the request, and leaves the server's update to run on
                    Logger.WriteLine ("HTTP: " + e, Logger.Severity.Error);
                    var body = ErrorBody (string.Empty, string.Empty, "Failed to handle the request: " + e.Message, string.Empty);
                    connection.Complete (slot, BuildResponse (500, body, slot.Close, null));
                }
            }
        }

        void Handle (Connection connection, Connection.Slot slot, HttpRequest request)
        {
            CheckHost (request);
            var path = request.Target;
            var query = path.IndexOf ('?');
            if (query >= 0)
                path = path.Substring (0, query);
            if (path == "/" || path == "/openapi.json") {
                if (request.Method != "GET")
                    throw new RequestException (405, path + " takes GET") { Allow = "GET" };
                // The root is a health check, with an empty body
                var body = path == "/" ? null : definitions.OpenApi;
                connection.Complete (slot, BuildResponse (200, body, slot.Close, null));
                return;
            }
            ulong id;
            var route = definitions.Routes.Find (request.Method, path, out id);
            var call = new Schema.KRPC.ProcedureCall { Service = route.Service, Procedure = route.Procedure.Name };
            if (route.Instance)
                call.Arguments.Add (new Schema.KRPC.Argument { Position = 0, Value = Codec.EncodeObject (id) });
            AddArguments (route, ParseBody (request), call);
            var message = new Schema.KRPC.Request { Id = nextId++ };
            message.Calls.Add (call);
            // When the bytes arrived, not when they were parsed, so that a request parsed in a
            // later update does not measure the wait for that update as the caller's turnaround
            var arrived = connection.Client.Stream.LastArrival;
            queued.Enqueue (new Pending {
                Request = message,
                Route = route,
                Connection = connection,
                Slot = slot,
                Arrived = arrived != 0 ? arrived : Stopwatch.GetTimestamp ()
            });
        }

        // A web page can send a request to the server through the browser. A host name other
        // than localhost is how a DNS rebinding attack reaches it, and an Origin header that
        // differs from the host is a page on another site
        static void CheckHost (HttpRequest request)
        {
            var host = request.Header ("host");
            if (host == null)
                throw new RequestException (400, "The request has no Host header");
            var name = host;
            if (name.StartsWith ("[", StringComparison.Ordinal)) {
                var end = name.IndexOf (']');
                name = end > 0 ? name.Substring (1, end - 1) : name;
            } else if (name.IndexOf (':') >= 0) {
                name = name.Substring (0, name.IndexOf (':'));
            }
            IPAddress address;
            if (!string.Equals (name, "localhost", StringComparison.OrdinalIgnoreCase) && !IPAddress.TryParse (name, out address))
                throw new RequestException (403, "The host " + name + " is not allowed. Use localhost or an IP address");
            var origin = request.Header ("origin");
            if (origin != null && origin != "http://" + host)
                throw new RequestException (403, "Requests from " + origin + " are not allowed");
        }

        static object ParseBody (HttpRequest request)
        {
            if (request.Body.Length == 0)
                return null;
            var contentType = request.Header ("content-type");
            if (contentType == null || contentType.Split (';') [0].Trim ().ToLowerInvariant () != "application/json")
                throw new RequestException (415, "The body must have the content type application/json");
            string text;
            try {
                text = new UTF8Encoding (false, true).GetString (request.Body);
            } catch (ArgumentException) {
                throw new RequestException (400, "The body is not valid UTF-8");
            }
            try {
                return Json.Parse (text);
            } catch (JsonException e) {
                throw new RequestException (400, "Malformed JSON: " + e.Message);
            }
        }

        // The body's values, by parameter name. A parameter left out takes its default value
        void AddArguments (Route route, object body, Schema.KRPC.ProcedureCall call)
        {
            var fields = body == null ? new Dictionary<string, object> () : body as Dictionary<string, object>;
            if (fields == null)
                throw new RequestException (400, "The body must be a JSON object");
            foreach (var key in fields.Keys)
                if (!route.Parameters.ContainsKey (key))
                    throw new RequestException (400, route.Procedure.Name + " has no parameter " + key);
            foreach (var entry in route.Parameters.OrderBy (x => x.Value)) {
                var parameter = route.Procedure.Parameters [entry.Value];
                object value;
                if (!fields.TryGetValue (entry.Key, out value)) {
                    if (!parameter.HasDefaultValue)
                        throw new RequestException (400, "Missing parameter " + entry.Key);
                    continue;
                }
                var argument = new Schema.KRPC.Argument { Position = (uint)entry.Value };
                if (value == null) {
                    if (!parameter.Type.Nullable)
                        throw new RequestException (400, "Parameter " + entry.Key + " cannot be null");
                    argument.IsNull = true;
                } else {
                    argument.Value = definitions.Codec.Encode (value, parameter.Type, "Parameter " + entry.Key);
                }
                call.Arguments.Add (argument);
            }
        }

        void Render (Route route, Schema.KRPC.Response response, out int status, out string body)
        {
            var error = response.Error;
            Schema.KRPC.ProcedureResult result = null;
            if (error == null) {
                result = response.Results [0];
                error = result.Error;
            }
            if (error != null) {
                // An argument the procedure rejects is the caller's error
                status = error.Service == "KRPC" && error.Exception.StartsWith ("Argument", StringComparison.Ordinal) ? 400 : 500;
                body = ErrorBody (error.Service, error.Exception, error.Description, error.StackTrace);
                return;
            }
            status = 200;
            var returnType = route.Procedure.ReturnType;
            if (returnType == null || returnType.Code == Schema.KRPC.Type.Types.TypeCode.None)
                body = "{}";
            else
                body = Json.Write (Routes.Entries ("value", result.IsNull ? null : definitions.Codec.Decode (result.Value, returnType)));
        }

        static string ErrorBody (string service, string name, string description, string stackTrace)
        {
            return Json.Write (Routes.Entries ("error", Routes.Entries (
                "service", service,
                "name", name,
                "description", description,
                "stack_trace", stackTrace)));
        }

        static byte[] BuildResponse (RequestException exception, bool close)
        {
            var body = ErrorBody (string.Empty, string.Empty, exception.Message, string.Empty);
            return BuildResponse (exception.Status, body, close, exception.Allow);
        }

        // A null body is an empty one
        static byte[] BuildResponse (int status, string body, bool close, string allow)
        {
            var payload = body == null ? new byte [0] : Encoding.UTF8.GetBytes (body + "\n");
            var head = new StringBuilder ();
            head.Append ("HTTP/1.1 ").Append (status.ToString (CultureInfo.InvariantCulture)).Append (' ').Append (Reason (status)).Append ("\r\n");
            if (body != null)
                head.Append ("Content-Type: application/json; charset=utf-8\r\n");
            head.Append ("Content-Length: ").Append (payload.Length.ToString (CultureInfo.InvariantCulture)).Append ("\r\n");
            if (allow != null)
                head.Append ("Allow: ").Append (allow).Append ("\r\n");
            if (close)
                head.Append ("Connection: close\r\n");
            head.Append ("\r\n");
            var headBytes = Encoding.ASCII.GetBytes (head.ToString ());
            var result = new byte [headBytes.Length + payload.Length];
            headBytes.CopyTo (result, 0);
            payload.CopyTo (result, headBytes.Length);
            return result;
        }

        static string Reason (int status)
        {
            switch (status) {
            case 200:
                return "OK";
            case 400:
                return "Bad Request";
            case 403:
                return "Forbidden";
            case 404:
                return "Not Found";
            case 405:
                return "Method Not Allowed";
            case 413:
                return "Content Too Large";
            case 415:
                return "Unsupported Media Type";
            case 431:
                return "Request Header Fields Too Large";
            case 501:
                return "Not Implemented";
            case 505:
                return "HTTP Version Not Supported";
            default:
                return "Internal Server Error";
            }
        }
    }
}
