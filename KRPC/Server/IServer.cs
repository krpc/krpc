using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace KRPC.Server
{
	public interface IServer
	{
		void Start();
		void Stop();
		INetworkStream GetClientStream(int clientId);
		IEnumerable<int> GetConnectedClientIds();
	}
}
