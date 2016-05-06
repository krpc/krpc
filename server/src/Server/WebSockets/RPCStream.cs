using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.WebSockets
{
    sealed class RPCStream : Message.RPCStream
    {
        /// <summary>
        /// Opcode for the payload fragments.
        /// </summary>
        OpCode fragmentsOpCode;

        /// <summary>
        /// Payload fragments for the current message if it is divided into multiple frames.
        /// </summary>
        readonly IList<byte[]> fragments = new List<byte[]> ();

        public RPCStream (IStream<byte,byte> stream) : base (stream)
        {
        }

        public override void Write (Response value)
        {
            var message = value.ToProtobufMessage ();
            var bufferStream = new MemoryStream ();
            message.WriteTo (bufferStream);
            var payload = bufferStream.ToArray ();
            var frame = new Frame (OpCode.Binary, payload);
            Stream.Write (frame.Header.ToBytes ());
            Stream.Write (frame.Payload);
        }

        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            //FIXME: should not fail if decoding a partial frame
            //FIXME: Close the connection on framing exception
            var frame = Frame.FromBytes (data, offset, length);

            // Get the op code for the first frame of the message
            var opCode = frame.Header.OpCode;
            if (frame.Header.OpCode == OpCode.Continue)
                opCode = fragmentsOpCode;

            // Get the payload for the entire message
            byte[] payload;
            if (!frame.Header.FinalFragment) {
                // Haven't received all the frames yet
                fragmentsOpCode = opCode;
                fragments.Add (frame.Payload);
                return (int)frame.Header.Length;
            }
            if (frame.Header.OpCode == fragmentsOpCode) {
                // Got the last frame, so combine them into the complete message
                payload = new byte[fragments.Sum (x => x.Length)];
                int index = 0;
                foreach (var fragment in fragments) {
                    Array.Copy (fragment, 0, payload, index, fragment.Length);
                    index += fragment.Length;
                    fragments.Clear ();
                }
            } else
                // Message only has one frame
                payload = frame.Payload;

            // Handle the message
            switch (opCode) {
            case OpCode.Binary:
                try {
                    request = Schema.KRPC.Request.Parser.ParseFrom (frame.Payload).ToMessage ();
                } catch (InvalidProtocolBufferException) {
                    // Incomplete request, send a close frame with a protocol error
                    Stream.Write (Frame.Close (1002, "Malformed protocol buffer message").ToBytes ());
                    //FIXME: Close the connection
                }
                break;
            case OpCode.Ping:
                // Send pong with copy of ping's payload
                Stream.Write (new Frame (OpCode.Pong, payload).ToBytes ());
                break;
            case OpCode.Close:
                if (frame.Header.Length >= 2)
                    // Send close with copy of status
                    Stream.Write (new Frame (OpCode.Close, new [] { payload [0], payload [1] }).ToBytes ());
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
