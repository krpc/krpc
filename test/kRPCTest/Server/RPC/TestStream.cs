using System;
using System.IO;
using KRPC.Server;

namespace KRPCTest.Server.RPC
{
    public class TestStream : IStream<byte,byte>
    {
        MemoryStream write_stream;
        MemoryStream read_stream;

        public TestStream (MemoryStream read_stream, MemoryStream write_stream)
        {
            this.read_stream = read_stream;
            this.write_stream = write_stream;
            if (read_stream != null) {
                read_stream.Seek (0, SeekOrigin.Begin);
            }
        }

        public bool DataAvailable {
            get {
                return read_stream.Position < read_stream.Length;
            }
        }

        public byte Read ()
        {
            return (byte)read_stream.ReadByte ();
        }

        public int Read (byte[] buffer, int offset)
        {
            return read_stream.Read (buffer, offset, buffer.Length - offset);
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            return read_stream.Read (buffer, offset, size);
        }

        public void Write (byte value)
        {
            throw new NotImplementedException ();
        }

        public void Write (byte[] buffer)
        {
            write_stream.Write (buffer, 0, buffer.Length);
        }

        public void Close ()
        {
            throw new NotImplementedException ();
        }
    }
}

