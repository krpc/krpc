using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KRPC.Utils;

namespace KRPC.Server.TCP
{
    sealed class TCPServer : IServer<byte,byte>
    {
        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionEventArgs<byte,byte>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<byte,byte>> OnClientConnected;
        #pragma warning disable 0067
        public event EventHandler<ClientActivityEventArgs<byte,byte>> OnClientActivity;
        #pragma warning restore 0067
        public event EventHandler<ClientDisconnectedEventArgs<byte,byte>> OnClientDisconnected;

        /// <summary>
        /// A name for the server.
        /// </summary>
        readonly string name;
        /// <summary>
        /// Port that the server listens on for new connections. If set to 0,
        /// a port number with be automatically chosen.
        /// </summary>
        ushort listenPort;
        /// <summary>
        /// The actual local port number of the server. Will be identical to
        /// port, unless port was set to 0.
        /// </summary>
        ushort actualPort;
        /// <summary>
        /// Thread used to poll for new connections.
        /// </summary>
        Thread listenerThread;
        TcpListener tcpListener;
        /// <summary>
        /// Event used to wait for the TCP listener to start
        /// </summary>
        volatile AutoResetEvent startedEvent;
        /// <summary>
        /// True if the listenerThread is running.
        /// </summary>
        volatile bool running;
        /// <summary>
        /// Connected clients.
        /// </summary>
        List<TCPClient> clients = new List<TCPClient> ();
        /// <summary>
        /// Clients requesting a connection. Must be locked before accessing.
        /// </summary>
        List<TCPClient> pendingClients = new List<TCPClient> ();
        object pendingClientsLock = new object ();
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        /// <summary>
        /// Create a TCP server. After Start() is called, the server will listen for
        /// connections to the specified local address and port number.
        /// </summary>
        public TCPServer (string serverName, IPAddress address, ushort port)
        {
            name = serverName;
            ListenAddress = address;
            listenPort = port;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void Start ()
        {
            if (running) {
                Logger.WriteLine ("TCPServer(" + name + "): start requested, but server is already running", Logger.Severity.Warning);
                return;
            }
            Logger.WriteLine ("TCPServer(" + name + "): starting", Logger.Severity.Debug);
            tcpListener = new TcpListener (ListenAddress, listenPort);
            try {
                tcpListener.Start ();
            } catch (SocketException exn) {
                string socketError = "socket error '" + exn.SocketErrorCode + "': " + exn.Message;
                Logger.WriteLine ("TCPServer(" + name + "): failed to start server; " + socketError, Logger.Severity.Error);
                throw new ServerException (socketError);
            }
            var endPoint = (IPEndPoint)tcpListener.LocalEndpoint;
            actualPort = (ushort)endPoint.Port;
            startedEvent = new AutoResetEvent (false);
            listenerThread = new Thread (ListenerThread);
            listenerThread.Start ();
            startedEvent.WaitOne (500);
            if (!running) {
                Logger.WriteLine ("TCPServer(" + name + "): failed to start server, timed out waiting for TcpListener to start", Logger.Severity.Error);
                listenerThread.Abort ();
                listenerThread.Join ();
                tcpListener = null;
                throw new ServerException ("Failed to start server, timed out waiting for TcpListener to start");
            }
            EventHandlerExtensions.Invoke (OnStarted, this);
            Logger.WriteLine ("TCPServer(" + name + "): started successfully");
            if (ListenAddress.ToString () == "0.0.0.0")
                Logger.WriteLine ("TCPServer(" + name + "): listening on all local network interfaces", Logger.Severity.Debug);
            else
                Logger.WriteLine ("TCPServer(" + name + "): listening on local address " + Address, Logger.Severity.Debug);
            Logger.WriteLine ("TCPServer(" + name + "): listening on port " + actualPort, Logger.Severity.Debug);
        }

        public void Stop ()
        {
            if (!running)
                return;

            Logger.WriteLine ("TCPServer(" + name + "): stop requested", Logger.Severity.Debug);
            tcpListener.Stop ();
            if (!listenerThread.Join (3000))
                throw new ServerException ("Failed to stop TCP listener thread (timed out after 3 seconds)");

            // Close all client connections
            foreach (var client in pendingClients) {
                Logger.WriteLine ("TCPServer(" + name + "): cancelling pending connection to client (" + client.Address + ")", Logger.Severity.Debug);
                DisconnectClient (client, true);
            }
            foreach (var client in clients) {
                Logger.WriteLine ("TCPServer(" + name + "): closing connection to client (" + client.Address + ")", Logger.Severity.Debug);
                DisconnectClient (client);
            }
            pendingClients.Clear ();
            clients.Clear ();
            Logger.WriteLine ("TCPServer(" + name + "): all client connections closed");

            // Exited cleanly
            running = false;
            Logger.WriteLine ("TCPServer(" + name + "): stopped successfully");

            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        public void Update ()
        {
            // Remove disconnected clients
            for (int i = clients.Count - 1; i >= 0; i--) {
                var client = clients [i];
                if (!client.Connected) {
                    clients.RemoveAt (i);
                    DisconnectClient (client);
                }
            }

            // Process pending clients
            lock (pendingClientsLock) {
                if (pendingClients.Count > 0) {
                    var stillPendingClients = new List<TCPClient> ();
                    foreach (var client in pendingClients) {
                        // Trigger OnClientRequestingConnection events to verify the connection
                        var args = new ClientRequestingConnectionEventArgs<byte,byte> (client);
                        EventHandlerExtensions.Invoke (OnClientRequestingConnection, OnClientRequestingConnection, args);

                        // Deny the connection
                        if (args.Request.ShouldDeny) {
                            Logger.WriteLine ("TCPServer(" + name + "): client connection denied (" + client.Address + ")", Logger.Severity.Warning);
                            DisconnectClient (client, true);
                        }

                        // Allow the connection
                        if (args.Request.ShouldAllow) {
                            Logger.WriteLine ("TCPServer(" + name + "): client connection accepted (" + client.Address + ")");
                            clients.Add (client);
                            EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<byte,byte> (client));
                        }

                        // Still pending, will either be denied or allowed on a subsequent called to Update
                        if (args.Request.StillPending) {
                            stillPendingClients.Add (client);
                        }
                    }
                    pendingClients = stillPendingClients;
                }
            }
        }

        public string Address {
            get { return ListenAddress + ":" + Port; }
        }

        const string localClientOnlyText = "Local clients only";
        const string anyClientText = "Any client";
        const string subnetAllowedText = "Subnet {0}";
        const string unknownClientsAllowedText = "Unknown visibility";

        public string Info {
            get {
                if (IPAddress.IsLoopback (ListenAddress))
                    return localClientOnlyText;
                else if (ListenAddress == IPAddress.Any)
                    return anyClientText;
                try {
                    var subnet = NetworkInformation.GetSubnetMask (ListenAddress);
                    return string.Format (subnetAllowedText, subnet);
                } catch (NotImplementedException) {
                } catch (ArgumentException) {
                } catch (DllNotFoundException) {
                }
                return unknownClientsAllowedText;
            }
        }

        public bool Running {
            get { return running; }
        }

        public IEnumerable<IClient<byte,byte>> Clients {
            get {
                foreach (var client in clients)
                    yield return client;
            }
        }

        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                for (int i = 0; i < clients.Count; i++)
                    read += clients [i].Stream.BytesRead;
                return read;
            }
        }

        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                for (int i = 0; i < clients.Count; i++)
                    written += clients [i].Stream.BytesWritten;
                return written;
            }
        }

        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            foreach (var client in clients)
                client.Stream.ClearStats ();
        }

        /// <summary>
        /// Port number that the server listens on. Server must be restarted for changes to take effect.
        /// </summary>
        public ushort Port {
            get { return actualPort; }
            set { listenPort = value; }
        }

        /// <summary>
        /// Local address that the server listens on. Server must be restarted for changes to take effect.
        /// </summary>
        public IPAddress ListenAddress { get; set; }

        [SuppressMessage ("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void ListenerThread ()
        {
            try {
                try {
                    running = true;
                    startedEvent.Set ();
                    while (true) {
                        // Block until a client connects to the server
                        var client = tcpListener.AcceptTcpClient ();
                        Logger.WriteLine ("TCPServer(" + name + "): client requesting connection (" + client.Client.RemoteEndPoint + ")", Logger.Severity.Debug);
                        // Add to pending clients
                        lock (pendingClientsLock) {
                            pendingClients.Add (new TCPClient (client));
                        }
                    }
                } catch (SocketException e) {
                    if (e.SocketErrorCode == SocketError.Interrupted)
                        Logger.WriteLine ("TCPServer(" + name + "): listener stopped", Logger.Severity.Debug);
                    else
                        throw;
                }
            } catch (Exception e) {
                Logger.WriteLine ("TCPServer(" + name + "): caught exception, listener stopped", Logger.Severity.Error);
                Logger.WriteLine (e.GetType ().Name);
                Logger.WriteLine (e.Message);
                Logger.WriteLine (e.StackTrace);
                tcpListener.Stop ();
            }
        }

        void DisconnectClient (IClient<byte,byte> client, bool noEvent = false)
        {
            var clientAddress = client.Address;
            try {
                var stream = client.Stream;
                closedClientsBytesRead += stream.BytesRead;
                closedClientsBytesWritten += stream.BytesWritten;
            } catch (ClientDisconnectedException) {
            }
            client.Close ();
            if (!noEvent)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<byte,byte> (client));
            Logger.WriteLine ("TCPServer(" + name + "): client disconnected (" + clientAddress + ")");
        }
    }
}
