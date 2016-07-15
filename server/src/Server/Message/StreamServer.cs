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
        public event EventHandler<ClientRequestingConnectionEventArgs<NoMessage,StreamMessage>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<NoMessage,StreamMessage>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityEventArgs<NoMessage,StreamMessage>> OnClientActivity;
        public event EventHandler<ClientDisconnectedEventArgs<NoMessage,StreamMessage>> OnClientDisconnected;

        IServer<byte,byte> server;
        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamMessage>> clients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamMessage>> ();
        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamMessage>> pendingClients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamMessage>> ();

        protected StreamServer (IServer<byte,byte> innerServer)
        {
            server = innerServer;
            server.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            server.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            server.OnClientRequestingConnection += HandleClientRequestingConnection;
            server.OnClientConnected += HandleClientConnected;
            server.OnClientConnected += HandleClientActivity;
            server.OnClientDisconnected += HandleClientDisconnected;
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

        public string Address {
            get { return server.Address; }
        }

        public string Info {
            get { return server.Info; }
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

        void HandleClientConnected (object sender, ClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<NoMessage,StreamMessage> (client));
        }

        void HandleClientActivity (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientActivity, this, new ClientActivityEventArgs<NoMessage,StreamMessage> (client));
        }

        void HandleClientDisconnected (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<NoMessage,StreamMessage> (client));
        }

        protected abstract IClient<NoMessage,StreamMessage> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args);

        /// <summary>
        /// When a client requests a connection, check the hello message,
        /// then trigger RPCServer.OnClientRequestingConnection to get response of delegates
        /// </summary>
        public virtual void HandleClientRequestingConnection (object sender, ClientRequestingConnectionEventArgs<byte,byte> args)
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
            var handler = OnClientRequestingConnection;
            if (handler != null) {
                var client = pendingClients [args.Client];
                var subArgs = new ClientRequestingConnectionEventArgs<NoMessage,StreamMessage> (client);
                handler (this, subArgs);
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
