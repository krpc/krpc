using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class StreamStream : Message.StreamStream
    {
        readonly CodedOutputStream codedOutputStream;

        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
        }

        public override void Write (StreamUpdate value)
        {
            codedOutputStream.WriteMessage (value.ToProtobufMessage ());
            codedOutputStream.Flush ();
        }
    }
}
