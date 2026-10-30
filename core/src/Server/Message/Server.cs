using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Abstract server for the connections of a byte server. Each connection carries one
    /// client's session at a time.
    /// </summary>
    public abstract class Server : IServer<Request,Response>
    {
        /// <summary>
        /// Event handler for when the server starts.
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event handler for when the server stops.
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event handler for when a new client requests a connection.
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs<Request,Response>> OnClientRequestingConnection;

        /// <summary>
        /// Event handler for when a new client has connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<Request,Response>> OnClientConnected;

        /// <summary>
        /// Event handler when client activity occurs. Not triggered.
        /// </summary>
        #pragma warning disable 0067
        public event EventHandler<ClientActivityEventArgs<Request,Response>> OnClientActivity;
        #pragma warning restore 0067

        /// <summary>
        /// Event handler when a client disconnects.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<Request,Response>> OnClientDisconnected;

        const double STOP_DRAIN_TIMEOUT = 1;

        internal IServer<byte,byte> InnerServer { get; private set; }

        readonly Dictionary<IClient<byte,byte>,Connection> connections = new Dictionary<IClient<byte,byte>,Connection> ();
        readonly List<Connection> updating = new List<Connection> ();
        readonly List<Client> endedSessions = new List<Client> ();

        /// <summary>
        /// Construct a server from a raw byte server.
        /// </summary>
        protected Server (IServer<byte,byte> innerServer)
        {
            InnerServer = innerServer;
            InnerServer.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            InnerServer.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            // Connections are approved once their handshake has been read
            InnerServer.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            InnerServer.OnClientConnected += (s, e) => connections [e.Client] = CreateConnection (e.Client);
            InnerServer.OnClientDisconnected += HandleClientDisconnected;
        }

        /// <summary>
        /// Create the connection for a byte client.
        /// </summary>
        internal abstract Connection CreateConnection (IClient<byte,byte> client);

        /// <summary>
        /// Seconds a partial message may go without a new byte before its connection ends, on
        /// a transport that sets no limit of its own.
        /// </summary>
        internal double StallTimeout { get; set; } = 30;

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            InnerServer.Start ();
        }

        /// <summary>
        /// Stop the server. Blocks for up to 1 second while the connections drain, so that
        /// each client reads its disconnect message.
        /// </summary>
        public void Stop ()
        {
            foreach (var connection in connections.Values)
                connection.Stop ();
            RaiseSessionsEnded ();
            var timer = Stopwatch.StartNew ();
            while (timer.ElapsedSeconds () < STOP_DRAIN_TIMEOUT) {
                var draining = false;
                foreach (var connection in connections.Values) {
                    if (connection.Draining) {
                        connection.Update ();
                        draining |= connection.Draining;
                    }
                }
                if (!draining)
                    break;
                Thread.Sleep (1);
            }
            foreach (var connection in connections.Values)
                connection.EndDrain ();
            InnerServer.Stop ();
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            InnerServer.Update ();
            RaiseSessionsEnded ();
            updating.Clear ();
            updating.AddRange (connections.Values);
            foreach (var connection in updating)
                connection.Update ();
            RaiseSessionsEnded ();
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public virtual string Address {
            get { return InnerServer.Address; }
        }

        /// <summary>
        /// Information about the server. Depends on the server protocol.
        /// </summary>
        public string Info {
            get { return InnerServer.Info; }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return InnerServer.Running; }
        }

        /// <summary>
        /// Clients connected to the server.
        /// </summary>
        public IEnumerable<IClient<Request,Response>> Clients {
            get {
                foreach (var connection in connections.Values) {
                    var session = connection.Session;
                    if (session != null)
                        yield return session;
                }
            }
        }

        /// <summary>
        /// Number of bytes received from clients.
        /// </summary>
        public ulong BytesRead {
            get { return InnerServer.BytesRead; }
        }

        /// <summary>
        /// Number of bytes sent to clients.
        /// </summary>
        public ulong BytesWritten {
            get { return InnerServer.BytesWritten; }
        }

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ClearStats ()
        {
            InnerServer.ClearStats ();
        }

        /// <summary>
        /// Ask the handlers whether to allow a connection. Allowed if there are none.
        /// </summary>
        internal void RequestingConnection (ClientRequestingConnectionEventArgs<Request,Response> args)
        {
            var handler = OnClientRequestingConnection;
            if (handler == null)
                args.Request.Allow ();
            else
                handler (this, args);
        }

        internal void SessionStarted (Client client)
        {
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<Request,Response> (client));
        }

        /// <summary>
        /// Queue the event for a session that has ended. A session can end while the core is
        /// polling its clients for requests, so the event is raised on the next update.
        /// </summary>
        internal void SessionEnded (Client client)
        {
            endedSessions.Add (client);
        }

        void RaiseSessionsEnded ()
        {
            if (endedSessions.Count == 0)
                return;
            var ended = endedSessions.ToArray ();
            endedSessions.Clear ();
            foreach (var client in ended)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<Request,Response> (client));
        }

        void HandleClientDisconnected (object sender, ClientEventArgs<byte,byte> args)
        {
            Connection connection;
            if (!connections.TryGetValue (args.Client, out connection))
                return;
            connection.Disconnected ();
            connections.Remove (args.Client);
            RaiseSessionsEnded ();
        }
    }
}
