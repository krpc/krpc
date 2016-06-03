using System;
using System.IO;

namespace KRPC.Server
{
    class ByteOutputStreamAdapter : Stream
    {
        readonly IStream<byte,byte> stream;

        public ByteOutputStreamAdapter (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException ();
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            stream.Write (buffer, offset, count);
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
            get { throw new NotImplementedException (); }
        }

        public override void SetLength (long value)
        {
            throw new NotImplementedException ();
        }

        public override long Position {
            get { throw new NotImplementedException (); }
            set { throw new NotImplementedException (); }
        }

        public override long Seek (long offset, SeekOrigin origin)
        {
            throw new NotImplementedException ();
        }

        public override void Flush ()
        {
        }
    }
}
