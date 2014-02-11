using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using KRPC.Utils;

namespace KRPC.Server
{
	/// <summary>
	/// A simpler TCP server, supporting multiple client connections
	/// Each client is identified by a unique integer
	/// </summary>
	public class TCPServer : IServer
	{
		/// <summary>
		/// Listens for client connections over TCP
		/// </summary>
		private TcpListener tcpListener;

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
		private bool running = false;

		/// <summary>
		/// Number of bytes to read from a client in each call to Read on the client stream
		/// </summary>
		private const int bufferSize = 4096;

		/// <summary>
		/// Port that the server listens on for new connections.
		/// </summary>
		private int port;

		public TCPServer (int port)
		{
			this.port = port;
		}

		/// <summary>
		/// Start the server and listen for client connections
		/// </summary>
	    public void Start()
	    {
			if (running)
				return;
			running = true;
	    	tcpListener = new TcpListener(IPAddress.Any, port);
	        listenerThread = new Thread(new ThreadStart(ConnectionListener));
	        listenerThread.Start();
	    }

		/// <summary>
		/// Stop the server. Close all client connections and stop listening for new connections.
		/// </summary>
		public void Stop()
		{
			// TODO: implement
		}

		/// <summary>
		/// Entry point for thread that listens for client connections.
		/// Spawns a new thread for each new connection
		/// </summary>
		private void ConnectionListener()
		{
			tcpListener.Start();

			// The next client id to allocate
			int clientId = 0;
		  	
		  	while (running)
		  	{
		    	// Blocks until a client has connected to the server
		    	TcpClient client = tcpListener.AcceptTcpClient();

				System.Console.WriteLine ("TCPServer: Client " + clientId + " connected");

				lock (clientsLock)
				{
					clients[clientId] = client;
					clientStreams[clientId] = new NetworkStreamWrapper(client.GetStream());
				}
				clientId++;
		  	}
		}

		/// <summary>
		/// Return a list of connected clients.
		/// </summary>
		public IEnumerable<int> GetConnectedClientIds ()
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
