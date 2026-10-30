using System;
using System.IO;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    sealed class TCPStream : IStream<byte,byte>, IHalfClosableStream
    {
        readonly NetworkStream stream;
        readonly Socket socket;

        public TCPStream (NetworkStream innerStream, Socket innerSocket)
        {
            stream = innerStream;
            socket = innerSocket;
        }

        public bool DataAvailable {
            get {
                try {
                    return stream.DataAvailable;
                } catch (IOException) {
                    return false;
                } catch (ObjectDisposedException) {
                    return false;
                } catch (SocketException) {
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
                throw new ClientDisconnectedException ();
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
                throw new ClientDisconnectedException ();
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
                throw new ClientDisconnectedException ();
            }
        }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            BytesRead = 0;
            BytesWritten = 0;
        }

        /// <summary>
        /// Whether the write side has been closed. The socket then reports itself disconnected.
        /// </summary>
        public bool HalfClosed { get; private set; }

        public void HalfClose ()
        {
            try {
                HalfClosed = true;
                socket.Shutdown (SocketShutdown.Send);
            } catch (SocketException e) {
                throw new ServerException (e.Message);
            } catch (ObjectDisposedException) {
                throw new ClientDisconnectedException ();
            }
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}
