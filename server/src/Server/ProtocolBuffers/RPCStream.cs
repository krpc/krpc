using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using KRPC.Server.Message;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class RPCStream : Message.RPCStream
    {
        readonly CodedOutputStream codedOutputStream;

        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
        }

        public override void Write (Response value)
        {
            codedOutputStream.WriteMessage (value.ToProtobufMessage ());
            codedOutputStream.Flush ();
        }

        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            try {
                var codedStream = new CodedInputStream (data, offset, length);
                // Get the protobuf message size
                var size = (int)codedStream.ReadUInt32 ();
                var totalSize = (int)codedStream.Position + size;
                // Check if enough data is available, if not then delay the decoding
                if (length < totalSize)
                    return 0;
                // Decode the request
                request = Schema.KRPC.Request.Parser.ParseFrom (codedStream).ToMessage ();
                return totalSize;
            } catch (InvalidProtocolBufferException e) {
                throw new MalformedRequestException (e.Message);
            }
        }
    }
}
