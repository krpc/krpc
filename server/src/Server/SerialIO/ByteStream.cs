using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using KRPC.IO.Ports;

namespace KRPC.Server.SerialIO
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    sealed class ByteStream : IStream<byte,byte>
    {
        SerialPort stream;
        byte[] readBuffer;
        int readBufferOffset;

        public ByteStream (SerialPort innerStream, byte[] buffer = null)
        {
            stream = innerStream;
            readBuffer = buffer;
        }

        public bool DataAvailable {
            get {
                try {
                    return
                        (readBuffer != null) ||
                        (stream != null && stream.IsOpen && stream.BytesToRead > 0);
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

        int ReadBufferedData (byte[] buffer, int offset, int size) {
            var remainingReadBufferData = readBuffer.Length - readBufferOffset;
            size = Math.Min(size, remainingReadBufferData);
            Array.Copy(readBuffer, readBufferOffset, buffer, offset, size);
            readBufferOffset += size;
            if (size == remainingReadBufferData) {
                readBuffer = null;
                readBufferOffset= 0;
            }
            return size;
        }

        public int Read (byte[] buffer, int offset)
        {
            if (readBuffer != null)
                return ReadBufferedData (buffer, offset, buffer.Length - offset);
            if (stream == null)
                throw new ClientDisconnectedException ();
            try {
                var size = stream.Read (buffer, offset, buffer.Length - offset);
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
            if (readBuffer != null)
                return ReadBufferedData (buffer, offset, size);
            if (stream == null)
                throw new ClientDisconnectedException ();
            try {
                size = stream.Read (buffer, offset, size);
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

        [SuppressMessage ("Gendarme.Rules.Naming", "ParameterNamesShouldMatchOverriddenMethodRule")]
        public void Write (byte[] buffer)
        {
            Write (buffer, 0, buffer.Length);
        }

        public void Write (byte[] buffer, int offset, int size)
        {
            if (stream == null)
                throw new ClientDisconnectedException ();
            try {
                stream.Write (buffer, offset, size);
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
            stream = null;
        }
    }
}
