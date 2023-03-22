using System.Diagnostics.CodeAnalysis;
using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.WebSockets
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class StreamStream : Message.StreamStream
    {
        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (StreamUpdate value)
        {
            using (var bufferStream = new MemoryStream ()) {
                value.ToProtobufMessage ().WriteTo (bufferStream);
                bufferStream.Flush ();
                var payload = bufferStream.ToArray ();
                var frame = new Frame (OpCode.Binary, payload);
                Stream.Write (frame.Header.ToBytes ());
                Stream.Write (frame.Payload);
            }
        }
    }
}
