using System;
using System.IO;
using KRPC.Server;

namespace KRPCTest.Server
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
            var size = read_stream.Read (buffer, offset, buffer.Length - offset);
            BytesRead += size;
            return size;
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            size = read_stream.Read (buffer, offset, size);
            BytesRead += size;
            return size;
        }

        public void Write (byte value)
        {
            throw new NotSupportedException ();
        }

        public void Write (byte[] buffer)
        {
            var size = buffer.Length;
            write_stream.Write (buffer, 0, size);
            BytesWritten += size;
        }

        public long BytesRead { get; private set; }

        public long BytesWritten { get; private set; }

        public void Close ()
        {
            throw new NotSupportedException ();
        }
    }
}

