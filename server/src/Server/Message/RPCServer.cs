using System;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.Message
{
    /// <summary>
    /// Abstract RPC server for receiving requests and sending responses over a byte server.
    /// </summary>
    abstract class RPCServer : IServer<Request,Response>
    {
        const double defaultTimeout = 0.1;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionEventArgs<Request,Response>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<Request,Response>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event, unless the underlying server does.
        /// </summary>
        public event EventHandler<ClientActivityEventArgs<Request,Response>> OnClientActivity;
        public event EventHandler<ClientDisconnectedEventArgs<Request,Response>> OnClientDisconnected;

        internal IServer<byte,byte> Server { get; private set; }

        readonly Dictionary<IClient<byte,byte>,IClient<Request,Response>> clients = new Dictionary<IClient<byte, byte>, IClient<Request,Response>> ();
        readonly Dictionary<IClient<byte,byte>,IClient<Request,Response>> pendingClients = new Dictionary<IClient<byte, byte>, IClient<Request,Response>> ();
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

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

        public IEnumerable<IClient<Request,Response>> Clients {
            get {
                foreach (var client in clients.Values) {
                    yield return client;
                }
            }
        }

        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                foreach (var client in clients.Values)
                    read += client.Stream.BytesRead;
                return read;
            }
        }

        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                foreach (var client in clients.Values)
                    written += client.Stream.BytesWritten;
                return written;
            }
        }

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
