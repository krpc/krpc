using System;
using System.IO;
using KRPC.Service.Messages;
using KRPC.Server.ProtocolBuffers;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : IStream<NoMessage,StreamMessage>
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
            stream.Write (Encoder.EncodeStreamMessage (value));
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

