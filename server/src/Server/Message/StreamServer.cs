using System;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Abstract Stream server for sending stream messages over a byte server.
    /// </summary>
    abstract class StreamServer : IServer<NoMessage,StreamUpdate>
    {
        const double defaultTimeout = 0.1;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionEventArgs<NoMessage,StreamUpdate>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<NoMessage,StreamUpdate>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityEventArgs<NoMessage,StreamUpdate>> OnClientActivity;
        public event EventHandler<ClientDisconnectedEventArgs<NoMessage,StreamUpdate>> OnClientDisconnected;

        internal IServer<byte,byte> Server { get; private set; }

        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamUpdate>> clients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamUpdate>> ();
        readonly Dictionary<IClient<byte,byte>,IClient<NoMessage,StreamUpdate>> pendingClients = new Dictionary<IClient<byte,byte>, IClient<NoMessage,StreamUpdate>> ();

        protected StreamServer (IServer<byte,byte> innerServer)
        {
            Server = innerServer;
            Server.OnStarted += (s, e) => EventHandlerExtensions.Invoke (OnStarted, this);
            Server.OnStopped += (s, e) => EventHandlerExtensions.Invoke (OnStopped, this);
            Server.OnClientRequestingConnection += HandleClientRequestingConnection;
            Server.OnClientConnected += HandleClientConnected;
            Server.OnClientConnected += HandleClientActivity;
            Server.OnClientDisconnected += HandleClientDisconnected;
        }

        public void Start ()
        {
            Server.Start ();
        }

        public void Stop ()
        {
            Server.Stop ();
        }

        public void Update ()
        {
            Server.Update ();
        }

        public virtual string Address {
            get { return Server.Address; }
        }

        public string Info {
            get { return Server.Info; }
        }

        public bool Running {
            get { return Server.Running; }
        }

        public IEnumerable<IClient<NoMessage,StreamUpdate>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        public ulong BytesRead {
            get { return Server.BytesRead; }
        }

        public ulong BytesWritten {
            get { return Server.BytesWritten; }
        }

        public void ClearStats ()
        {
            Server.ClearStats ();
        }

        void HandleClientConnected (object sender, ClientEventArgs<byte,byte> args)
        {
            // Note: pendingClients and clients dictionaries are updated from HandleClientRequestingConnection
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<NoMessage,StreamUpdate> (client));
        }

        void HandleClientActivity (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            EventHandlerExtensions.Invoke (OnClientActivity, this, new ClientActivityEventArgs<NoMessage,StreamUpdate> (client));
        }

        void HandleClientDisconnected (object sender, ClientEventArgs<byte,byte> args)
        {
            var client = clients [args.Client];
            clients.Remove (args.Client);
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<NoMessage,StreamUpdate> (client));
        }

        protected abstract IClient<NoMessage,StreamUpdate> CreateClient (object sender, ClientRequestingConnectionEventArgs<byte,byte> args);

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
                var address = client.Address;
                var subArgs = new ClientRequestingConnectionEventArgs<NoMessage,StreamUpdate> (client);
                handler (this, subArgs);
                if (subArgs.Request.ShouldAllow) {
                    args.Request.Allow ();
                    clients [args.Client] = client;
                    Logger.WriteLine ("StreamServer: client connection allowed (" + address + ")", Logger.Severity.Debug);
                } else if (subArgs.Request.ShouldDeny) {
                    args.Request.Deny ();
                    Logger.WriteLine ("StreamServer: client connection denied (" + address + ")", Logger.Severity.Debug);
                } else {
                    pendingClients.Remove (args.Client);
                    Logger.WriteLine ("StreamServer: client connection still pending (" + address + ")", Logger.Severity.Debug);
                }
            } else {
                args.Request.Allow ();
                clients [args.Client] = pendingClients [args.Client];
                pendingClients.Remove (args.Client);
                Logger.WriteLine ("StreamServer: client connection allowed (" + args.Client.Address + ")", Logger.Severity.Debug);
            }
        }
    }
}
