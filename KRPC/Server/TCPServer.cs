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

		public int Port {
			get { return port; }
		}

		public IPAddress EndPoint {
			get { return endPoint; }
		}

		/// <summary>
		/// IP address from which incomming connections are allowed.
		/// </summary>
		private IPAddress endPoint;

		/// <summary>
		/// Port that the server listens on for new connections.
		/// </summary>
		private int port;

		/// <summary>
		/// Create a TCP server that will listen for connections from endPoint on the given port.
		/// Start() must be called to start listening for connections.
		/// </summary>
		public TCPServer (IPAddress endPoint, int port)
		{
			this.endPoint = endPoint;
			this.port = port;
		}

		/// <summary>
		/// Start the server and listen for client connections
		/// </summary>
	    public void Start()
		{
			if (running) {
				System.Console.WriteLine ("[kRPC] TCPServer: start requested, but server is already running");
				return;
			}
			System.Console.WriteLine ("[kRPC] TCPServer: starting");
			listenerThread = new Thread(new ThreadStart(ConnectionListener));
			listenerThread.Start();
	    }

		/// <summary>
		/// Stop the server. Close all client connections and stop listening for new connections.
		/// </summary>
		public void Stop()
		{
			System.Console.WriteLine ("[kRPC] TCPServer: stop requested");
			listenerThread.Abort ();
		}

		/// <summary>
		/// Entry point for thread that listens for client connections.
		/// Spawns a new thread for each new connection
		/// </summary>
		private void ConnectionListener()
		{
			TcpListener tcpListener = new TcpListener(endPoint, port);
			tcpListener.Start();
			System.Console.WriteLine ("[kRPC] TCPServer: listening on port " + port);
			System.Console.WriteLine ("[kRPC] TCPServer: accepting connections from " + endPoint);
			System.Console.WriteLine ("[kRPC] TCPServer: started successfully");

			// The next client id to allocate
			int clientId = 0;
		  	
			try
			{
				running = true;
				while (true)
				{	
					// Blocks until a client has connected to the server
			    	TcpClient client = tcpListener.AcceptTcpClient();
					System.Console.WriteLine ("[kRPC] TCPServer: client " + clientId + " connected (" + client.Client.RemoteEndPoint + ")");

					lock (clientsLock)
					{
						clients[clientId] = client;
						clientStreams[clientId] = new NetworkStreamWrapper(client.GetStream());
					}
					clientId++;
				}
			} catch (ThreadAbortException) {
				System.Console.WriteLine ("[kRPC] TCPServer: stopping...");
			} finally {
				tcpListener.Stop ();

				lock (clientsLock) {
					clients.Clear ();
					clientStreams.Clear ();
				}

				System.Console.WriteLine ("[kRPC] TCPServer: stopped");
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
