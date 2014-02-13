using System;
using System.Net.Sockets;

namespace KRPC.Server
{
	public class ClientRequestingConnectionArgs : EventArgs 
	{
		Socket client;
		INetworkStream stream;

		public ClientRequestingConnectionArgs(Socket client, INetworkStream stream)
		{
			this.client = client;
			this.stream = stream;
		}

		public Socket Client {
			get { return client; }
		}
		public INetworkStream Stream {
			get { return stream; }
		}

		private bool allow = false;
		private bool deny = false;

		public bool ShouldAllow {
			get { return allow && !deny; }
		}

		public bool ShouldDeny {
			get { return !ShouldAllow; }
		}

		public void Allow () {
			allow = true;
		}

		public void Deny () {
			deny = true;
		}
	}
}

