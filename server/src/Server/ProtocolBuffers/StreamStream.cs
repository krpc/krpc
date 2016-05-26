using Google.Protobuf;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : Message.StreamStream
    {
        readonly CodedOutputStream codedOutputStream;

        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputStreamAdapter (stream));
        }

        public override void Write (StreamMessage value)
        {
            codedOutputStream.WriteMessage (value.ToProtobufMessage ());
            codedOutputStream.Flush ();
        }
    }
}
