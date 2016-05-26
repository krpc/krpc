using System;
using System.IO;
using KRPC.Server;

namespace KRPC.Test.Server
{
    public class TestStream : IStream<byte,byte>
    {
        MemoryStream writeStream;
        MemoryStream readStream;

        public TestStream (byte[] readBytes, byte[] writeBytes = null)
        {
            readStream = new MemoryStream (readBytes);
            if (writeBytes != null)
                writeStream = new MemoryStream (writeBytes);
        }

        public TestStream (MemoryStream readStream, MemoryStream writeStream = null)
        {
            this.readStream = readStream;
            this.writeStream = writeStream;
            if (readStream != null)
                readStream.Seek (0, SeekOrigin.Begin);
        }

        public bool DataAvailable {
            get { return !Closed && readStream.Position < readStream.Length; }
        }

        public byte Read ()
        {
            if (Closed)
                throw new InvalidOperationException ();
            return (byte)readStream.ReadByte ();
        }

        public int Read (byte[] buffer, int offset)
        {
            if (Closed)
                throw new InvalidOperationException ();
            var size = readStream.Read (buffer, offset, buffer.Length - offset);
            BytesRead += (ulong)size;
            return size;
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            if (Closed)
                throw new InvalidOperationException ();
            size = readStream.Read (buffer, offset, size);
            BytesRead += (ulong)size;
            return size;
        }

        public void Write (byte value)
        {
            throw new NotSupportedException ();
        }

        public void Write (byte[] buffer)
        {
            Write (buffer, 0, buffer.Length);
        }

        public void Write (byte[] buffer, int offset, int size)
        {
            if (Closed || writeStream == null)
                throw new InvalidOperationException ();
            writeStream.Write (buffer, offset, size);
            BytesWritten += (ulong)size;
        }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            BytesRead = 0;
            BytesWritten = 0;
        }

        public bool Closed { get; set; }

        public void Close ()
        {
            readStream = null;
            writeStream = null;
            Closed = true;
        }
    }
}

