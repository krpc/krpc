using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;

namespace KRPC.Server.TCP
{
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInTypeNameRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class TCPStream : IStream<byte,byte>
    {
        readonly NetworkStream stream;

        public TCPStream (NetworkStream innerStream)
        {
            stream = innerStream;
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

        [SuppressMessage ("Gendarme.Rules.Naming", "ParameterNamesShouldMatchOverriddenMethodRule")]
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

        public void Close ()
        {
            stream.Close ();
        }
    }
}
