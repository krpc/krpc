using System;
using System.IO;
using KRPC.Schema.KRPC;
using Google.Protobuf;

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
            var bufferStream = new MemoryStream ();
            value.WriteDelimitedTo (bufferStream);
            stream.Write (bufferStream.ToArray ());
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

        public void ClearStats ()
        {
            stream.ClearStats ();
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}

