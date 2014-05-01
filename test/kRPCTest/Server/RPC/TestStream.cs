using System;
using System.IO;
using KRPC.Server;

namespace KRPCTest.Server.RPC
{
    public class TestStream : IStream<byte,byte>
    {
        MemoryStream stream;

        public TestStream (MemoryStream stream)
        {
            this.stream = stream;
            stream.Seek (0, SeekOrigin.Begin);
        }

        public bool DataAvailable {
            get {
                return stream.Position < stream.Length;
            }
        }

        public byte Read ()
        {
            return (byte)stream.ReadByte ();
        }

        public int Read (byte[] buffer, int offset)
        {
            return stream.Read (buffer, offset, buffer.Length - offset);
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            return stream.Read (buffer, offset, size);
        }

        public void Write (byte value)
        {
            throw new NotImplementedException ();
        }

        public void Write (byte[] buffer)
        {
            stream.Write (buffer, 0, buffer.Length);
        }

        public void Close ()
        {
            throw new NotImplementedException ();
        }
    }
}

