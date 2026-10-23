using System;
using System.IO;

namespace KRPC.Server.SerialIO
{
    sealed class ByteStream : IStream<byte,byte>
    {
        BufferedPort port;

        public ByteStream (BufferedPort innerPort)
        {
            port = innerPort;
        }

        public bool DataAvailable {
            get {
                try {
                    return port != null && port.IsOpen && port.BytesAvailable > 0;
                } catch (IOException) {
                    return false;
                } catch (TimeoutException) {
                    return false;
                } catch (ObjectDisposedException) {
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
            if (port == null)
                throw new ClientDisconnectedException ();
            try {
                var size = port.Read (buffer, offset, buffer.Length - offset);
                BytesRead += (ulong)size;
                return size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (TimeoutException e) {
                throw new ServerException (e.Message);
            } catch (ObjectDisposedException) {
                throw new ClientDisconnectedException ();
            }
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            if (port == null)
                throw new ClientDisconnectedException ();
            try {
                size = port.Read (buffer, offset, size);
                BytesRead += (ulong)size;
                return size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (TimeoutException e) {
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
            if (port == null)
                throw new ClientDisconnectedException ();
            try {
                port.Write (buffer, offset, size);
                BytesWritten += (ulong)size;
            } catch (IOException e) {
                throw new ServerException (e.Message);
            } catch (TimeoutException e) {
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

        public void Close ()
        {
            port = null;
        }
    }
}
