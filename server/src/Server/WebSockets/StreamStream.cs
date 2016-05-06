using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.WebSockets
{
    sealed class StreamStream : Message.StreamStream
    {
        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (StreamMessage value)
        {
            var message = value.ToProtobufMessage ();
            var bufferStream = new MemoryStream ();
            message.WriteTo (bufferStream);
            var payload = bufferStream.ToArray ();
            var frame = new Frame (OpCode.Binary, payload);
            Stream.Write (frame.Header.ToBytes ());
            Stream.Write (frame.Payload);
        }
    }
}
