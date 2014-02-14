using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Server
{
    /// <summary>
    /// A simpler TCP server, supporting multiple client connections
    /// Each client is identified by a unique integer
    /// </summary>
    //TODO: check for client disconnects
    //TODO: cleaner handling of client identifiers (this integer approach is horrible)
    public class TCPServer : IServer
    {
        public event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;
        public event EventHandler<ClientRequestingConnectionArgs> OnInteractiveClientRequestingConnection;

        //TODO: is read access to this thread safe?!
        TcpListener tcpListener;

        /// <summary>
        /// Thread that listens for client connections
        /// </summary>
        private Thread listenerThread;

        /// <summary>
        /// Mapping from client identifiers to client connections
        /// </summary>
        private Dictionary<int,TcpClient> clients = new Dictionary<int, TcpClient>();

        /// <summary>
        /// Mapping from client identifiers to network streams
        /// </summary>
        private Dictionary<int,INetworkStream> clientStreams = new Dictionary<int, INetworkStream>();

        /// <summary>
        /// Lock protecting the client dictionaries
        /// </summary>
        private readonly object clientsLock = new object();

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        private volatile bool running = false;

        public bool Running {
            get { return running; }
        }

        /// <summary>
        /// The local port number that the server listens on. If set to 0,
        /// returns the actual local port number when the server is started.
        /// </summary>
        /// <value>The port.</value>
        public int Port {
            get {
                return ((IPEndPoint) tcpListener.LocalEndpoint).Port;
            }
            set {
                if (!Running)
                    port = value;
            }
        }

        /// <summary>
        /// The local address that the server listens on. If set to IPAddress.Any,
        /// returns the actual local IP address when the server is started.
        /// </summary>
        public IPAddress LocalAddress {
            get {
                //FIXME: returns 0.0.0.0 instead of actual ip address, when local address is set to IPAddress.Any and the server has started
                return ((IPEndPoint) tcpListener.LocalEndpoint).Address;
            }
            set {
                if (!Running)
                    localAddress = value;
            }
        }

        /// <summary>
        /// IP address from which incomming connections are allowed.
        /// </summary>
        private IPAddress localAddress;

        /// <summary>
        /// Port that the server listens on for new connections.
        /// </summary>
        private int port;

        /// <summary>
        /// Create a TCP server. After Start() is called, the server will listen for
        /// connections to the specified local address and port number.
        /// </summary>
        public TCPServer (IPAddress localAddress, int port)
        {
            this.localAddress = localAddress;
            this.port = port;
        }

        /// <summary>
        /// Start the server and listen for client connections
        /// </summary>
        public void Start()
        {
            if (running) {
                Logger.WriteLine("TCPServer: start requested, but server is already running");
                return;
            }
            Logger.WriteLine("TCPServer: starting");
            tcpListener = new TcpListener(localAddress, port);
            listenerThread = new Thread(new ThreadStart(ConnectionListener));
            listenerThread.Start();
        }

        /// <summary>
        /// Stop the server. Close all client connections and stop listening for new connections.
        /// </summary>
        public void Stop()
        {
            Logger.WriteLine("TCPServer: stop requested");
            listenerThread.Abort ();
        }

        /// <summary>
        /// Entry point for thread that listens for client connections.
        /// Spawns a new thread for each new connection
        /// </summary>
        private void ConnectionListener()
        {
            bool started = false;
            try
            {
                tcpListener.Start();
                started = true;
                Logger.WriteLine("TCPServer: listening on local address " + ((IPEndPoint)tcpListener.LocalEndpoint).Address);
                Logger.WriteLine("TCPServer: listening on port " + ((IPEndPoint)tcpListener.LocalEndpoint).Port);
                Logger.WriteLine("TCPServer: started successfully");

                // The next client id to allocate
                int clientId = 0;

                running = true;
                while (true)
                {    
                    // Block until a client connects to the server
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Logger.WriteLine("TCPServer: client requesting connection (" + client.Client.RemoteEndPoint + ")");

                    // Trigger OnClientRequestingConnection events to verify the connection
                    var attempt = new ClientRequestingConnectionArgs(client.Client, new NetworkStreamWrapper(client.GetStream()));
                    OnClientRequestingConnection(this, attempt);
                    if (attempt.ShouldDeny) {
                        Logger.WriteLine("TCPServer: client connection denied (" + client.Client.RemoteEndPoint + ")");
                        client.Close();
                        continue;
                    } else {
                        Logger.WriteLine("TCPServer: client connection denied (" + client.Client.RemoteEndPoint + ")");
                    }

                    // Trigger OnInteractiveClientRequestingConnection events to verify the connection
                    attempt = new ClientRequestingConnectionArgs(client.Client, new NetworkStreamWrapper(client.GetStream()));
                    OnInteractiveClientRequestingConnection(this, attempt);
                    if (attempt.ShouldDeny) {
                        Logger.WriteLine("TCPServer: client connection denied by player (" + client.Client.RemoteEndPoint + ")");
                        client.Close();
                        continue;
                    }

                    // Connection was successful, update the server state
                    Logger.WriteLine("TCPServer: client connection accepted (" + client.Client.RemoteEndPoint + ")");
                    lock (clientsLock)
                    {
                        clients[clientId] = client;
                        clientStreams[clientId] = new NetworkStreamWrapper(client.GetStream());
                    }
                    clientId++;
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
                if (!started)
                    Logger.WriteLine ("TCPServer: failed to start");
                else
                    tcpListener.Stop ();

                // Close all client connections
                TcpClient[] tcpClients;
                lock (clientsLock) {
                    tcpClients = clients.Values.ToArray ();
                    clients.Clear ();
                    clientStreams.Clear ();
                }
                foreach (TcpClient client in tcpClients) {
                    Logger.WriteLine("TCPServer: closing connection to client (" + client.Client.RemoteEndPoint + ")");
                    client.Close();
                }
                Logger.WriteLine("TCPServer: all client connections closed");

                // Exited cleanly
                Logger.WriteLine("TCPServer: stopped");
                running = false;
            }
        }

        /// <summary>
        /// Return a list of connected clients.
        /// </summary>
        public ICollection<int> GetConnectedClientIds ()
        {
            lock (clientsLock) {
                int[] ids = new int[clients.Keys.Count];
                clients.Keys.CopyTo (ids, 0);
                return ids;
            }
        }

        /// <summary>
        /// Gets a stream object for communication with the client
        /// </summary>
        public INetworkStream GetClientStream (int clientId)
        {
            lock (clientsLock) {
                return clientStreams[clientId];
            }
        }
    }
}
