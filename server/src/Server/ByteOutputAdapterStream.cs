using System;
using System.IO;

namespace KRPC.Server
{
    sealed class ByteOutputAdapterStream : Stream
    {
        readonly IStream<byte,byte> innerStream;

        public ByteOutputAdapterStream (IStream<byte,byte> stream)
        {
            innerStream = stream;
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException ();
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            try {
                innerStream.Write (buffer, offset, count);
            } catch (ClientDisconnectedException e) {
                throw new ObjectDisposedException ("Client disconnected", e);
            }
        }

        public override bool CanRead {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override long Length {
            get { throw new InvalidOperationException (); }
        }

        public override void SetLength (long value)
        {
            throw new InvalidOperationException ();
        }

        public override long Position {
            get { throw new InvalidOperationException (); }
            set { throw new InvalidOperationException (); }
        }

        public override long Seek (long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException ();
        }

        public override void Flush ()
        {
        }
    }
}
