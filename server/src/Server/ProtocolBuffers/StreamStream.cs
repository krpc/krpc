using System;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : Message.StreamStream
    {
        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        protected override byte[] Encode (StreamMessage response)
        {
            return Encoder.EncodeStreamMessage (response);
        }
    }
}
