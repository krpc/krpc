using System;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using System.IO;

namespace KRPC.Server.WebSockets
{
    sealed class RPCStream : Message.RPCStream
    {
        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (Response value)
        {
            var message = value.ToProtobufResponse ();
            var bufferStream = new MemoryStream ();
            message.WriteTo (bufferStream);
            var payload = bufferStream.ToArray ();
            var frame = new Frame (OpCode.Binary, payload);
            Stream.Write (frame.Header.ToBytes ());
            Stream.Write (frame.Payload);
        }

        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            Frame frame;
            try {
                frame = Frame.FromBytes (data, offset, length);
            } catch (FramingException) {
                //FIXME: Close the connection
                throw new NotImplementedException ();
            }
            switch (frame.Header.OpCode) {
            case OpCode.Binary:
                // Decode binary message as a protocol buffer request message
                try {
                    request = Schema.KRPC.Request.Parser.ParseFrom (frame.Payload).ToRequest ();
                } catch (InvalidProtocolBufferException) {
                    // Incomplete request, send a close frame with a protocol error
                    Stream.Write (Frame.Close (1002, "Malformed protocol buffer message").ToBytes ());
                    //FIXME: Close the connection
                }
                break;
            case OpCode.Ping:
                // Send pong with copy of ping's payload
                Stream.Write (new Frame (OpCode.Pong, frame.Payload).ToBytes ());
                break;
            case OpCode.Close:
                if (frame.Header.Length >= 2)
                    // Send close with copy of status
                    Stream.Write (new Frame (OpCode.Close, new [] { frame.Payload [0], frame.Payload [1] }).ToBytes ());
                else
                    // Send close with no status
                    Stream.Write (new Frame (OpCode.Close).ToBytes ());
                    //FIXME: close the connection
                break;
            case OpCode.Text:
                Stream.Write (Frame.Close (1003, "Text frames are not permitted").ToBytes ());
                //FIXME: Close the connection
                break;
            }
            return (int)frame.Header.Length;
        }
    }
}
