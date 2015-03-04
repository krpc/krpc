using System;
using System.IO;
using KRPC.Schema.KRPC;

namespace KRPC.Server.Stream
{
    sealed class StreamStream : IStream<byte,StreamResponse>
    {
        readonly IStream<byte,byte> stream;

        public StreamStream (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        public bool DataAvailable {
            get {
                return stream.DataAvailable;
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

        public void Write (StreamResponse value)
        {
            var tempBuffer = new MemoryStream ();
            value.WriteDelimitedTo (tempBuffer);
            stream.Write (tempBuffer.ToArray ());
        }

        public void Write (StreamResponse[] value)
        {
            throw new NotImplementedException ();
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}

