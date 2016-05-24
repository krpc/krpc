using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : Message.StreamStream
    {
        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (StreamMessage value)
        {
            var data = new MemoryStream ();
            value.ToProtobufMessage ().WriteDelimitedTo (data);
            var buffer = data.ToArray ();
            Stream.Write (buffer, 0, buffer.Length);
        }
    }
}
