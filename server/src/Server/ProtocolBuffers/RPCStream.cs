using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class RPCStream : Message.RPCStream
    {
        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        protected override byte[] Encode (Response response)
        {
            return Encoder.EncodeResponse (response);
        }

        protected override Request Decode (byte[] data, int start, int length)
        {
            return Encoder.DecodeRequest (data, start, length);
        }
    }
}
