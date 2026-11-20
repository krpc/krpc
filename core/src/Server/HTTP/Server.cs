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
        readonly AllowedOrigins allowedOrigins;
        readonly Dictionary<IClient<IByteStream>, Connection> connections = new Dictionary<IClient<IByteStream>, Connection> ();
        readonly List<Connection> updating = new List<Connection> ();
        Session session;
        bool sessionStarted;
        Definitions definitions;
        ulong nextId = 1;
        readonly Queue<Pending> queued = new Queue<Pending> ();
        readonly Dictionary<ulong, Pending> running = new Dictionary<ulong, Pending> ();

        /// <summary>
        /// Create a server that accepts HTTP connections through a TCP/IP server. Web pages
        /// from <paramref name="origins"/> may call it, besides pages it serves itself.
        /// </summary>
        public Server (TCPServer server, AllowedOrigins origins = null)
        {
            tcpServer = server;
            allowedOrigins = origins ?? new AllowedOrigins (null);
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
        /// The origins whose web pages may call the server.
        /// </summary>
        public AllowedOrigins AllowedOrigins {
            get { return allowedOrigins; }
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
            pending.Connection.Complete (pending.Slot, BuildResponse (status, body, pending.Slot));
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
                    var malformed = connection.Reserve (true);
                    connection.Complete (malformed, BuildResponse (e, malformed));
                    return;
                }
                if (request == null)
                    return;
                var slot = connection.Reserve (!request.KeepAlive);
                try {
                    Handle (connection, slot, request);
                } catch (RequestException e) {
                    connection.Complete (slot, BuildResponse (e, slot));
                } catch (System.Exception e) {
                    // An unexpected exception fails the request, and leaves the server's update to run on
                    Logger.WriteLine ("HTTP: " + e, Logger.Severity.Error);
                    var body = ErrorBody (string.Empty, string.Empty, "Failed to handle the request: " + e.Message, string.Empty);
                    connection.Complete (slot, BuildResponse (500, body, slot));
                }
            }
        }

        void Handle (Connection connection, Connection.Slot slot, HttpRequest request)
        {
            var host = CheckHost (request);
            CheckOrigin (request, host, slot);
            var path = request.Target;
            var query = path.IndexOf ('?');
            if (query >= 0)
                path = path.Substring (0, query);
            var fixedPath = path == "/" || path == "/openapi.json";
            if (request.Method == "OPTIONS") {
                var methods = fixedPath ? "GET" : definitions.Routes.Methods (path);
                connection.Complete (slot, BuildResponse (204, null, slot, Preflight (slot, methods)));
                return;
            }
            if (fixedPath) {
                if (request.Method != "GET")
                    throw new RequestException (405, path + " takes GET") { Allow = "GET" };
                // The root is a health check, with an empty body
                var body = path == "/" ? null :
                    definitions.OpenApi.Replace (OpenApi.ServerUrl, Json.Write ("http://" + host));
                connection.Complete (slot, BuildResponse (200, body, slot));
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

        // A host name other than the machine's own is how a DNS rebinding attack reaches the
        // server from a web page, as the page's own name pointed at the machine
        static string CheckHost (HttpRequest request)
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
            if (!IPAddress.TryParse (name, out address) && !MachineNames.Contains (name))
                throw new RequestException (403, "The host " + name + " is not allowed. Use localhost, an IP address or the name of the machine");
            return host;
        }

        static HashSet<string> machineNames;

        // localhost, and the name of the machine with and without .local
        static HashSet<string> MachineNames {
            get {
                if (machineNames == null) {
                    machineNames = new HashSet<string> (StringComparer.OrdinalIgnoreCase) { "localhost" };
                    try {
                        var name = Dns.GetHostName ();
                        machineNames.Add (name);
                        machineNames.Add (name + ".local");
                    } catch (System.Net.Sockets.SocketException) {
                    }
                }
                return machineNames;
            }
        }

        // A page the server does not serve itself has an Origin of another host, and may call
        // the server only from an allowed origin. Its response then names the origin, so the
        // browser lets the page read it
        void CheckOrigin (HttpRequest request, string host, Connection.Slot slot)
        {
            var origin = request.Header ("origin");
            if (origin == null || origin == "http://" + host)
                return;
            if (!allowedOrigins.Allows (origin))
                throw new RequestException (403, "Requests from " + origin + " are not allowed. Add the origin to the server's allowed origins");
            slot.Origin = origin;
        }

        // A browser asks before a cross-origin request with a JSON body, or a method other
        // than GET or POST
        static List<KeyValuePair<string, string>> Preflight (Connection.Slot slot, string methods)
        {
            var headers = new List<KeyValuePair<string, string>> ();
            if (slot.Origin == null) {
                headers.Add (new KeyValuePair<string, string> ("Allow", methods));
            } else {
                headers.Add (new KeyValuePair<string, string> ("Access-Control-Allow-Methods", methods));
                headers.Add (new KeyValuePair<string, string> ("Access-Control-Allow-Headers", "Content-Type"));
                headers.Add (new KeyValuePair<string, string> ("Access-Control-Max-Age", "600"));
            }
            return headers;
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

        static byte[] BuildResponse (RequestException exception, Connection.Slot slot)
        {
            var body = ErrorBody (string.Empty, string.Empty, exception.Message, string.Empty);
            List<KeyValuePair<string, string>> headers = null;
            if (exception.Allow != null)
                headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string> ("Allow", exception.Allow) };
            return BuildResponse (exception.Status, body, slot, headers);
        }

        // A 204 has no Content-Length, as RFC 9110 forbids it
        static byte[] BuildResponse (int status, string body, Connection.Slot slot, IList<KeyValuePair<string, string>> headers = null)
        {
            var payload = body == null ? new byte [0] : Encoding.UTF8.GetBytes (body + "\n");
            var head = new StringBuilder ();
            head.Append ("HTTP/1.1 ").Append (status.ToString (CultureInfo.InvariantCulture)).Append (' ').Append (Reason (status)).Append ("\r\n");
            if (body != null) {
                head.Append ("Content-Type: application/json; charset=utf-8\r\n");
                head.Append ("Content-Length: ").Append (payload.Length.ToString (CultureInfo.InvariantCulture)).Append ("\r\n");
            } else if (status != 204) {
                head.Append ("Content-Length: 0\r\n");
            }
            if (slot.Origin != null) {
                head.Append ("Access-Control-Allow-Origin: ").Append (slot.Origin).Append ("\r\n");
                head.Append ("Vary: Origin\r\n");
            }
            if (headers != null)
                foreach (var header in headers)
                    head.Append (header.Key).Append (": ").Append (header.Value).Append ("\r\n");
            if (slot.Close)
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
            case 204:
                return "No Content";
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
