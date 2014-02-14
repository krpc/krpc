using System;
using System.IO;
using System.Net.Sockets;
using KRPC.Server;

namespace KRPCTest
{
    public class MemoryStreamWrapper : INetworkStream
    {
        private MemoryStream stream;

        public MemoryStreamWrapper (MemoryStream stream)
        {
            this.stream = stream;
        }

        public bool DataAvailable {
            get {
                return stream.Position < stream.Length;
            }
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            return stream.Read(buffer, offset, size);
        }

        public NetworkStream GetUnderlyingNetworkStream ()
        {
            throw new NotImplementedException ();
        }
    }
}

