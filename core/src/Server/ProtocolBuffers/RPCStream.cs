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
                Schema.KRPC.Request message = null;
                var read = Utils.ReadMessage<Schema.KRPC.Request>(
                    ref message, Schema.KRPC.Request.Parser, data, offset, length);
                if (message != null)
                    request = message.ToMessage ();
                return read;
            } catch (System.InvalidOperationException e) {
                throw new MalformedRequestException (e.Message);
            } catch (InvalidProtocolBufferException e) {
                throw new MalformedRequestException (e.Message);
            }
        }
    }
}
