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
            var message = value.ToProtobufStreamMessage ();
            var data = new MemoryStream ();
            message.WriteDelimitedTo (data);
            Stream.Write (data.ToArray ());
        }
    }
}
