using System;
using System.IO;
using KRPC.Server;

namespace KRPC.Test.Server
{
    sealed class TestStream : IByteStream, IHalfClosableStream, IDisposable
    {
        MemoryStream outputStream;
        MemoryStream inputStream;

        public TestStream (byte[] input, byte[] output = null)
        {
            inputStream = new MemoryStream (input);
            if (output != null)
                outputStream = new MemoryStream (output);
        }

        public TestStream (MemoryStream input = null, MemoryStream output = null)
        {
            inputStream = input;
            outputStream = output;
            if (inputStream != null)
                inputStream.Seek (0, SeekOrigin.Begin);
        }

        bool disposed;

        public void Dispose ()
        {
            if (!disposed) {
                if (inputStream != null)
                    inputStream.Dispose ();
                if (outputStream != null)
                    outputStream.Dispose ();
                disposed = true;
            }
        }

        void CheckDisposed ()
        {
            if (disposed)
                throw new ObjectDisposedException (GetType ().Name);
        }

        public bool DataAvailable {
            get { return !Closed && inputStream.Position < inputStream.Length; }
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            CheckDisposed ();
            if (Closed)
                throw new InvalidOperationException ();
            size = inputStream.Read (buffer, offset, size);
            BytesRead += (ulong)size;
            return size;
        }

        public void Write (byte[] buffer)
        {
            CheckDisposed ();
            Write (buffer, 0, buffer.Length);
        }

        public void Write (byte[] buffer, int offset, int size)
        {
            CheckDisposed ();
            if (FailWrites)
                throw new ServerException ("Write failed");
            if (Closed || outputStream == null)
                throw new InvalidOperationException ();
            outputStream.Write (buffer, offset, size);
            BytesWritten += (ulong)size;
            Writes++;
        }

        /// <summary>
        /// The number of writes made to the stream.
        /// </summary>
        public int Writes { get; private set; }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            CheckDisposed ();
            BytesRead = 0;
            BytesWritten = 0;
        }

        public bool Closed { get; set; }

        /// <summary>
        /// Whether a write throws, as on a connection the peer has reset.
        /// </summary>
        public bool FailWrites { get; set; }

        /// <summary>
        /// Whether the write side of the stream has been closed.
        /// </summary>
        public bool HalfClosed { get; private set; }

        public void HalfClose ()
        {
            HalfClosed = true;
        }

        public void Close ()
        {
            CheckDisposed ();
            inputStream = null;
            outputStream = null;
            Closed = true;
        }
    }
}
