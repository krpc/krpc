using System;
using System.IO;
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
            try {
                var size = stream.Read (buffer, offset, buffer.Length - offset);
                BytesRead += (ulong)size;
                return size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (ObjectDisposedException) {
                throw new ServerException ("Connection closed");
            }
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            try {
                size = stream.Read (buffer, offset, size);
                BytesRead += (ulong)size;
                return size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (ObjectDisposedException) {
                throw new ServerException ("Connection closed");
            }
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
            try {
                stream.Write (buffer, offset, size);
                BytesWritten += (ulong)size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (ObjectDisposedException) {
                throw new ServerException ("Connection closed");
            }
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
