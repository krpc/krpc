using Google.Protobuf;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCStream : Message.RPCStream
    {
        readonly CodedOutputStream codedOutputStream;

        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputStreamAdapter (stream));
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
                int size = (int)codedStream.ReadUInt32 ();
                int totalSize = (int)codedStream.Position + size;
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
