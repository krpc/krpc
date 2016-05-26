using System;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Abstract Stream server for sending stream messages over a byte server.
    /// </summary>
    abstract class StreamServer : IServer<NoMessage,StreamMessage>
    {
        const double defaultTimeout = 0.1;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs<NoMessage,StreamMessage>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<NoMessage,StreamMessage>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityArgs<NoMessage,StreamMessage>> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs<NoMessage,StreamMessage>> OnClientDisconnected;

        IServer<byte,byte> server;
        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamMessage>> clients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamMessage>> ();
        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamMessage>> pendingClients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamMessage>> ();

        protected StreamServer (IServer<byte,byte> server)
        {
            this.server = server;
            server.OnStarted += (s, e) => {
                if (OnStarted != null)
                    OnStarted (this, EventArgs.Empty);
            };
            server.OnStopped += (s, e) => {
                if (OnStopped != null)
                    OnStopped (this, EventArgs.Empty);
            };
            server.OnClientRequestingConnection += HandleClientRequestingConnection;
            server.OnClientConnected += HandleClientConnected;
            server.OnClientConnected += HandleClientActivity;
            server.OnClientDisconnected += HandleClientDisconnected;
        }

        public IServer<byte,byte> Server {
            get { return server; }
        }

        public void Start ()
        {
            server.Start ();
        }

        public void Stop ()
        {
            server.Stop ();
        }

        public void Update ()
        {
            server.Update ();
        }

        public bool Running {
            get { return server.Running; }
        }

        public IEnumerable<IClient<NoMessage,StreamMessage>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        public ulong BytesRead {
            get { return server.BytesRead; }
        }

        public ulong BytesWritten {
            get { return server.BytesWritten; }
        }

        public void ClearStats ()
        {
            server.ClearStats ();
        }

        void HandleClientConnected (object sender, IClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            if (OnClientConnected != null) {
                var client = clients [args.Client];
                OnClientConnected (this, new ClientConnectedArgs<NoMessage,StreamMessage> (client));
            }
        }

        void HandleClientActivity (object sender, IClientEventArgs<byte,byte> args)
        {
            if (OnClientActivity != null) {
                var client = clients [args.Client];
                OnClientActivity (this, new ClientActivityArgs<NoMessage,StreamMessage> (client));
            }
        }

        void HandleClientDisconnected (object sender, IClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            if (OnClientDisconnected != null) {
                OnClientDisconnected (this, new ClientDisconnectedArgs<NoMessage,StreamMessage> (client));
            }
        }

        protected abstract IClient<NoMessage,StreamMessage> CreateClient (object sender, ClientRequestingConnectionArgs<byte,byte> args);

        /// <summary>
        /// When a client requests a connection, check the hello message,
        /// then trigger RPCServer.OnClientRequestingConnection to get response of delegates
        /// </summary>
        public virtual void HandleClientRequestingConnection (object sender, ClientRequestingConnectionArgs<byte,byte> args)
        {
            if (!pendingClients.ContainsKey (args.Client)) {
                var client = CreateClient (sender, args);
                if (args.Request.ShouldDeny)
                    return;
                if (client == null)
                    return;
                pendingClients [args.Client] = client;
            }

            // Client is in pending clients and passed hello message verification.
            // Invoke connection request events.
            if (OnClientRequestingConnection != null) {
                var client = pendingClients [args.Client];
                var subArgs = new ClientRequestingConnectionArgs<NoMessage,StreamMessage> (client);
                OnClientRequestingConnection (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    Logger.WriteLine ("StreamServer: Client connection allowed.");
                }
                if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("StreamServer: Client connection denied.", Logger.Severity.Warning);
                }
                if (!subArgs.Request.StillPending) {
                    pendingClients.Remove (args.Client);
                }
            } else {
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                Logger.WriteLine ("StreamServer: Client connection allowed.");
                pendingClients.Remove (args.Client);
            }
        }
    }
}
