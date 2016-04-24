using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCStream : Message.RPCStream
    {
        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (Response value)
        {
            Stream.Write (Encoder.EncodeResponse (value));
        }

        protected override Request Decode (byte[] data, int start, int length, ref int read)
        {
            return Encoder.DecodeRequest (data, start, length);
        }
    }
}
