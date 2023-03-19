using System;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Abstract RPC server for receiving requests and sending responses over a byte server.
    /// </summary>
    public abstract class RPCServer : IServer<Request,Response>
    {
        const double defaultTimeout = 0.1;

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
        /// Event handler when client activity occurs.
        /// </summary>
        /// <remarks>
        /// Does not trigger this event, unless the underlying server does.
        /// </remarks>
        public event EventHandler<ClientActivityEventArgs<Request,Response>> OnClientActivity;

        /// <summary>
        /// Event handler when a client disconnects.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<Request,Response>> OnClientDisconnected;

        internal IServer<byte,byte> Server { get; private set; }

        readonly Dictionary<IClient<byte,byte>,IClient<Request,Response>> clients = new Dictionary<IClient<byte, byte>, IClient<Request,Response>> ();
        readonly Dictionary<IClient<byte,byte>,IClient<Request,Response>> pendingClients = new Dictionary<IClient<byte, byte>, IClient<Request,Response>> ();
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        /// <summary>
        /// Construct an RPC server from a raw byte server
        /// </summary>
        protected RPCServer (IServer<byte,byte> innerServer)
        {
            Server = innerServer;
            Server.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            Server.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            Server.OnClientRequestingConnection += HandleClientRequestingConnection;
            Server.OnClientConnected += HandleClientConnected;
            Server.OnClientConnected += HandleClientActivity;
            Server.OnClientDisconnected += HandleClientDisconnected;
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            Server.Start ();
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop ()
        {
            Server.Stop ();
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            Server.Update ();
        }

        /// <summary>
        /// Address the server is listening on. Format depends on the server protocol.
        /// </summary>
        public virtual string Address {
            get { return Server.Address; }
        }

        /// <summary>
        /// Information about the server. Depends on the server protocol.
        /// </summary>
        public string Info {
            get { return Server.Info; }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return Server.Running; }
        }

        /// <summary>
        /// Clients conneted to the server.
        /// </summary>
        public IEnumerable<IClient<Request,Response>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        /// <summary>
        /// Number of bytes received from clients.
        /// </summary>
        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                foreach (var client in clients.Values)
                    read += client.Stream.BytesRead;
                return read;
            }
        }

        /// <summary>
        /// Number of bytes sent to clients.
        /// </summary>
        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                foreach (var client in clients.Values)
                    written += client.Stream.BytesWritten;
                return written;
            }
        }

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            foreach (var client in clients.Values)
                client.Stream.ClearStats ();
        }

        void HandleClientConnected (object sender, ClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<Request,Response> (client));
        }

        void HandleClientActivity (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientActivity, this, new ClientActivityEventArgs<Request,Response> (client));
        }

        void HandleClientDisconnected (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            var stream = client.Stream;
            closedClientsBytesRead += stream.BytesRead;
            closedClientsBytesWritten += stream.BytesWritten;
            clients.Remove (args.Client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<Request,Response> (client));
        }

        /// <summary>
        /// Create a client instance from a connection request event.
        /// </summary>
        protected abstract IClient<Request,Response> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args);

        /// <summary>
        /// When a client requests a connection, check and parse the hello message (which should
        /// consist of a header and a client name), then trigger RPCServer.OnClientRequestingConnection
        /// to get response of delegates
        /// </summary>
        public virtual void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
        {
            Logger.WriteLine ("Message.RPCServer: handling client connection request", Logger.Severity.Debug);
            if (!pendingClients.ContainsKey (args.Client)) {
                // A new client connection attempt. Try to create the client.
                var client = CreateClient (sender, args);
                // Check if creating the client denied the connection attempt
                if (args.Request.ShouldDeny)
                    return;
                // Check if a client was created - if not, retry again later
                if (client == null)
                    return;
                // Add the client to the pending connections
                pendingClients [args.Client] = client;
            }

            // Client is in pending clients and passed hello message verification.
            // Invoke connection request events.
            var handler = OnClientRequestingConnection;
            if (handler != null) {
                var client = pendingClients [args.Client];
                var subArgs = new ClientRequestingConnectionEventArgs<Request,Response> (client);
                handler (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    Logger.WriteLine ("Message.RPCServer: client connection allowed", Logger.Severity.Debug);
                } else if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("Message.RPCServer: client connection denied", Logger.Severity.Debug);
                } else {
                    Logger.WriteLine ("Message.RPCServer: client connection still pending", Logger.Severity.Debug);
                }
            } else {
                // No events configured, so allow the connection
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                Logger.WriteLine ("Message.RPCServer: client connection allowed", Logger.Severity.Debug);
                pendingClients.Remove (args.Client);
            }
        }
    }
}
