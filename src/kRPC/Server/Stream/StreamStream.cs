using System;
using System.IO;
using KRPC.Schema.KRPC;

namespace KRPC.Server.Stream
{
    sealed class StreamStream : IStream<byte,byte>
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
            return stream.Read ();
        }

        public int Read (byte[] buffer, int offset)
        {
            return stream.Read (buffer, offset);
        }

        public int Read (byte[] buffer, int offset, int size)
        {
            return stream.Read (buffer, offset, size);
        }

        public void Write (byte value)
        {
            stream.Write (value);
        }

        public void Write (byte[] value)
        {
            stream.Write (value);
        }

        public void Close ()
        {
            stream.Close ();
        }
    }
}

