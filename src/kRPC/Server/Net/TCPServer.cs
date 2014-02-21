using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Server.Net
{
    sealed class TCPServer : IServer<byte,byte>
    {
        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs<byte,byte>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs<byte,byte>> OnClientConnected;
        /// <summary>
        /// Does not trigger this event.
        /// </summary>
        public event EventHandler<ClientActivityArgs<byte,byte>> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs<byte,byte>> OnClientDisconnected;

        /// <summary>
        /// Local IP address on which the service listens.
        /// If set to localhost (127.0.0.1) the server with listen on
        /// the loopback device and only accept connections from the local machine.
        /// If set to IPAddress.Any, an available local
        /// address from one of the network adapters will be chosen.
                              /// </summary>
        private IPAddress address;
        /// <summary>
        /// Port that the server listens on for new connections. If set to 0,
        /// a port number with be automatically chosen.
        /// </summary>
        private ushort port;
        /// <summary>
        /// The actual local address of the server. Will be identical to
        /// localAdress, unless localAddress was set to IPAddress.Any.
        /// </summary>
        private IPAddress actualAddress;
        /// <summary>
        /// The actual local port number of the server. Will be identical to
        /// port, unless port was set to 0.
        /// </summary>
        private ushort actualPort;
        /// <summary>
        /// Thread used to poll for new connections.
        /// </summary>
        private Thread listenerThread;
        private TcpListener tcpListener;
        /// <summary>
        /// Event used to wait for the TCP listener to start
        /// </summary>
        private volatile AutoResetEvent startedEvent;
        /// <summary>
        /// True if the listenerThread is running.
        /// </summary>
        private volatile bool running = false;
        /// <summary>
        /// Connected clients.
        /// </summary>
        private List<TCPClient> clients = new List<TCPClient>();
        /// <summary>
        /// Clients requesting a connection. Must be locked before accessing.
        /// </summary>
        private List<TCPClient> pendingClients = new List<TCPClient>();
        private Object pendingClientsLock = new object();

        /// <summary>
        /// Create a TCP server. After Start() is called, the server will listen for
        /// connections to the specified local address and port number.
        /// </summary>
        public TCPServer (IPAddress address, ushort port)
        {
            this.address = address;
            this.port = port;
        }

        public void Start()
        {
            if (running) {
                Logger.WriteLine("TCPServer: start requested, but server is already running");
                return;
            }
            Logger.WriteLine("TCPServer: starting");
            tcpListener = new TcpListener (address, port);
            tcpListener.Start();
            IPEndPoint endPoint = (IPEndPoint)tcpListener.LocalEndpoint;
            actualAddress = endPoint.Address;
            actualPort = (ushort) endPoint.Port;
            startedEvent = new AutoResetEvent(false);
            listenerThread = new Thread(ListenerThread);
            listenerThread.Start();
            startedEvent.WaitOne (500);
            if (!running) {
                Logger.WriteLine("TCPServer: Failed to start server, timed out waiting for TcpListener to start");
                listenerThread.Abort ();
                listenerThread.Join ();
                tcpListener = null;
                return;
            }
            if (OnStarted != null)
                OnStarted (this, EventArgs.Empty);
            Logger.WriteLine("TCPServer: started successfully");
            Logger.WriteLine("TCPServer: listening on local address " + actualAddress);
            Logger.WriteLine("TCPServer: listening on port " + actualPort);
        }

        public void Stop()
        {
            Logger.WriteLine("TCPServer: stop requested");
            listenerThread.Abort ();
            // TODO: add timeout just in case...
            listenerThread.Join ();

            // Close all client connections
            foreach (var client in pendingClients) {
                Logger.WriteLine ("TCPServer: cancelling pending connection to client (" + client.Address + ")");
                DisconnectClient (client, noEvent: true);
            }
            foreach (var client in clients) {
                Logger.WriteLine ("TCPServer: closing connection to client (" + client.Address + ")");
                DisconnectClient (client);
            }
            pendingClients.Clear ();
            clients.Clear ();
            Logger.WriteLine("TCPServer: all client connections closed");

            // Exited cleanly
            running = false;
            Logger.WriteLine("TCPServer: stopped");

            if (OnStopped != null)
                OnStopped (this, EventArgs.Empty);
        }

        public void Update() {
            // Remove disconnected clients
            foreach (var client in clients.Where (x => !x.Connected).ToList ()) {
                clients.Remove (client);
                DisconnectClient (client);
            }

            // Process pending clients
            lock (pendingClientsLock) {
                var stillPendingClients = new List<TCPClient> ();
                foreach (var client in pendingClients) {
                    // Trigger OnClientRequestingConnection events to verify the connection
                    var args = new ClientRequestingConnectionArgs<byte,byte> (client);
                    if (OnClientRequestingConnection != null)
                        OnClientRequestingConnection (this, args);

                    // Deny the connection
                    if (args.Request.ShouldDeny) {
                        Logger.WriteLine ("TCPServer: client connection denied (" + client.Address + ")");
                        DisconnectClient (client, noEvent: true);
                    }

                    // Allow the connection
                    if (args.Request.ShouldAllow) {
                        Logger.WriteLine ("TCPServer: client connection accepted (" + client.Address + ")");
                        clients.Add (client);
                        if (OnClientConnected != null)
                            OnClientConnected (this, new ClientConnectedArgs<byte,byte> (client));
                    }

                    // Still pending, will either be denied or allowed on a subsequent called to Update
                    if (args.Request.StillPending) {
                        stillPendingClients.Add (client);
                    }
                }
                pendingClients = stillPendingClients;
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

        /// <summary>
        /// Port number that the server listens on. Server must be restarted for changes to take effect.
        /// </summary>
        public ushort Port {
            get { return actualPort; }
            set { port = value; }
        }

        /// <summary>
        /// Local address that the server listens on. Server must be restarted for changes to take effect.
        /// </summary>
        public IPAddress Address {
            //FIXME: returns 0.0.0.0 instead of actual ip address, when local address is set to IPAddress.Any and the server has started
            get { return actualAddress; }
            set { address = value; }
        }

        private void ListenerThread()
        {
            try
            {
                running = true;
                startedEvent.Set ();
                int nextClientUuid = 0;
                while (true)
                {
                    // Block until a client connects to the server
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Logger.WriteLine("TCPServer: client requesting connection (" + client.Client.RemoteEndPoint + ")");
                    // Add to pending clients
                    lock (pendingClientsLock) {
                        pendingClients.Add(new TCPClient(nextClientUuid, client));
                    }
                    nextClientUuid++;
                }
            } catch (ThreadAbortException) {
                // Stop() was called
                Logger.WriteLine("TCPServer: stopping...");
            } catch (Exception e) {
                //TODO: better error handling
                Console.WriteLine (e.Message);
                Console.Write (e.StackTrace);
            } finally {
                //Stop the tcp listener
                if (!running)
                    Logger.WriteLine ("TCPServer: failed to start");
                else if (tcpListener != null)
                    tcpListener.Stop ();
            }
        }

        private void DisconnectClient (IClient<byte,byte> client, bool noEvent = false) {
            client.Stream.Close ();
            client.Close ();
            if (!noEvent && OnClientDisconnected != null)
                OnClientDisconnected (this, new ClientDisconnectedArgs<byte,byte> (client));
        }
    }
}
