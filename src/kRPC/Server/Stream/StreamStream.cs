using System;
using System.IO;
using KRPC.Schema.KRPC;

namespace KRPC.Server.Stream
{
    sealed class StreamStream : IStream<byte,StreamMessage>
    {
        readonly IStream<byte,byte> stream;

        public StreamStream (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        public bool DataAvailable {
            get {
                throw new NotImplementedException ();
            }
        }

        public byte Read ()
        {
            throw new NotImplementedException ();
        }

        public int Read (byte[] buffer, int offset)
        {
            throw new NotImplementedException ();
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            throw new NotImplementedException ();
        }

        public void Write (StreamMessage value)
        {
            var tempBuffer = new MemoryStream ();
            value.WriteDelimitedTo (tempBuffer);
            stream.Write (tempBuffer.ToArray ());
        }

        public void Write (StreamMessage[] value)
        {
            throw new NotImplementedException ();
        }

        public ulong BytesRead {
            get { return stream.BytesRead; }
        }

        public ulong BytesWritten {
            get { return stream.BytesWritten; }
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}

