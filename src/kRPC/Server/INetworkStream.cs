using System;
using System.Net.Sockets;

namespace KRPC.Server
{
    public interface INetworkStream
    {
        int Read(byte[] buffer, int offset, int size);
        bool DataAvailable { get; }
        NetworkStream GetUnderlyingNetworkStream();
    }
}
