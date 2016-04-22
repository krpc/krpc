using System;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    sealed class TCPStream : IStream<byte,byte>
    {
        readonly NetworkStream stream;

        public TCPStream (NetworkStream stream)
        {
            this.stream = stream;
        }

        public bool DataAvailable {
            get {
                try {
                    return stream.DataAvailable;
                } catch {
                    return false;
                }
            }
        }

        public byte Read ()
        {
            throw new NotSupportedException ();
        }

        public int Read (byte[] buffer, int offset)
        {
            var size = stream.Read (buffer, offset, buffer.Length - offset);
            BytesRead += (ulong)size;
            return size;
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            size = stream.Read (buffer, offset, size);
            BytesRead += (ulong)size;
            return size;
        }

        public void Write (byte value)
        {
            throw new NotSupportedException ();
        }

        public void Write (byte[] buffer)
        {
            var size = buffer.Length;
            stream.Write (buffer, 0, size);
            BytesWritten += (ulong)size;
        }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            BytesRead = 0;
            BytesWritten = 0;
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}
