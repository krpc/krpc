using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace KRPC.Server
{
	public delegate void ClientRequestingConnectionHandler(Socket client, ConnectionAttempt attempt);

	public interface IServer
	{
		void Start();
		void Stop();
		bool Running {
			get;
		}
		INetworkStream GetClientStream(int clientId);
		ICollection<int> GetConnectedClientIds();

		event ClientRequestingConnectionHandler OnClientRequestingConnection;
	}
}
