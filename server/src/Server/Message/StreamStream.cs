using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class StreamStream : IStream<NoMessage,StreamMessage>
    {
        readonly IStream<byte,byte> stream;

        protected StreamStream (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        protected abstract byte[] Encode (StreamMessage response);

        public bool DataAvailable {
            get { throw new NotImplementedException (); }
        }

        public NoMessage Read ()
        {
            throw new NotImplementedException ();
        }

        public int Read (NoMessage[] buffer, int offset)
        {
            throw new NotImplementedException ();
        }

        public int Read (NoMessage[] buffer, int offset, int size)
        {
            throw new NotImplementedException ();
        }

        public void Write (StreamMessage value)
        {
            stream.Write (Encode (value));
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

