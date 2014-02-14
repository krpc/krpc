using System;
using System.Net.Sockets;

namespace KRPC.Server
{
	public class NetworkStreamWrapper : INetworkStream
	{
		private NetworkStream stream;

		public NetworkStreamWrapper (NetworkStream stream)
		{
			this.stream = stream;
		}

		public bool DataAvailable {
			get {
				return stream.DataAvailable;
			}
		}

		public int Read (byte[] buffer, int offset, int size)
		{
			return stream.Read(buffer, offset, size);
		}

		public NetworkStream GetUnderlyingNetworkStream ()
		{
			return stream;
		}
	}
}
