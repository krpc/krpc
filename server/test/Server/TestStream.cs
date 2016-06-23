using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using KRPC.Server;

namespace KRPC.Test.Server
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    sealed class TestStream : IStream<byte,byte>, IDisposable
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

        public byte Read ()
        {
            CheckDisposed ();
            if (Closed)
                throw new InvalidOperationException ();
            return (byte)inputStream.ReadByte ();
        }

        public int Read (byte[] buffer, int offset)
        {
            CheckDisposed ();
            if (Closed)
                throw new InvalidOperationException ();
            var size = inputStream.Read (buffer, offset, buffer.Length - offset);
            BytesRead += (ulong)size;
            return size;
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

        public void Write (byte value)
        {
            CheckDisposed ();
            throw new NotSupportedException ();
        }

        public void Write (byte[] buffer)
        {
            CheckDisposed ();
            Write (buffer, 0, buffer.Length);
        }

        public void Write (byte[] buffer, int offset, int size)
        {
            CheckDisposed ();
            if (Closed || outputStream == null)
                throw new InvalidOperationException ();
            outputStream.Write (buffer, offset, size);
            BytesWritten += (ulong)size;
        }

        public ulong BytesRead { get; private set; }

        public ulong BytesWritten { get; private set; }

        public void ClearStats ()
        {
            CheckDisposed ();
            BytesRead = 0;
            BytesWritten = 0;
        }

        public bool Closed { get; set; }

        public void Close ()
        {
            CheckDisposed ();
            inputStream = null;
            outputStream = null;
            Closed = true;
        }
    }
}
