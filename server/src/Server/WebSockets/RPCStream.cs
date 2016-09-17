using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Google.Protobuf;
using KRPC.Server.Message;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.WebSockets
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    sealed class RPCStream : Message.RPCStream
    {
        /// <summary>
        /// Whether the stream should just echo messages back to the client.
        /// Used for running the Autobahn tests.
        /// </summary>
        readonly bool shouldEcho;

        /// <summary>
        /// Op code for the fragements.
        /// OpCode.Close indicates we have not started receiving a fragmented message.
        /// </summary>
        OpCode fragmentsOpCode = OpCode.Close;

        /// <summary>
        /// Concatenated payloads for current fragmented message.
        /// </summary>
        readonly DynamicBuffer fragmentsPayload = new DynamicBuffer ();

        /// <summary>
        /// Position up to which the fragments payload has been verified as valid UTF8 (for text messages).
        /// </summary>
        int fragmentsVerifiedPosition;

        public RPCStream (IStream<byte,byte> stream, bool echo = false) : base (stream)
        {
            shouldEcho = echo;
        }

        public override void Write (Response value)
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

        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            int read = 0;
            while (length > 0) {
                // Read the next frame
                Frame frame;
                try {
                    frame = Frame.FromBytes (data, offset, length);
                } catch (FramingException e) {
                    Stream.Write (Frame.Close (e.Status, e.Message).ToBytes ());
                    Stream.Close ();
                    throw new MalformedRequestException (e.ToString ());
                } catch (NoRequestException) {
                    return read;
                }

                // Fail if rsv bits are set
                if (frame.Header.Rsv1 || frame.Header.Rsv2 || frame.Header.Rsv3) {
                    Logger.WriteLine ("WebSockets invalid message: RSV bit(s) set in frame header", Logger.Severity.Error);
                    Stream.Write (Frame.Close (1002).ToBytes ());
                    Stream.Close ();
                    break;
                }

                // Fail if a continue frame is received before a start frame
                if (frame.Header.OpCode == OpCode.Continue && fragmentsOpCode == OpCode.Close) {
                    Logger.WriteLine ("WebSockets invalid message: message fragment received out of order", Logger.Severity.Error);
                    Stream.Write (Frame.Close (1002).ToBytes ());
                    Stream.Close ();
                    break;
                }

                // Fail if a start frame is received before the fragmented message ends
                if (frame.Header.OpCode != OpCode.Continue && !frame.Header.IsControl && fragmentsOpCode != OpCode.Close) {
                    Logger.WriteLine ("WebSockets invalid message: message start fragment received out of order", Logger.Severity.Error);
                    Stream.Write (Frame.Close (1002).ToBytes ());
                    Stream.Close ();
                    break;
                }

                // Partial payload received
                if (frame.IsPartial) {
                    // Check that partially received text frames are valid UTF8
                    if (frame.Header.OpCode == OpCode.Text || (frame.Header.OpCode == OpCode.Continue && fragmentsOpCode == OpCode.Text)) {
                        fragmentsPayload.Append (frame.Payload, 0, frame.Payload.Length);
                        int truncatedCharLength = 0;
                        if (!Text.IsValidTruncatedUTF8 (fragmentsPayload.GetBuffer (), fragmentsVerifiedPosition, fragmentsPayload.Length - fragmentsVerifiedPosition, ref truncatedCharLength)) {
                            Logger.WriteLine ("WebSockets invalid message: malformed UTF8 string", Logger.Severity.Error);
                            Stream.Write (Frame.Close (1007, "Malformed UTF8 string").ToBytes ());
                            Stream.Close ();
                        }
                        fragmentsPayload.Length -= frame.Payload.Length;
                    }
                    break;
                }

                if (!frame.Header.IsControl && frame.Header.OpCode != OpCode.Continue && !frame.Header.FinalFragment)
                    fragmentsOpCode = frame.Header.OpCode;

                // Get the op code for the frame, or Binary if the frame is a fragment of a message
                var opCode = frame.Header.OpCode;
                if (frame.Header.OpCode == OpCode.Continue)
                    opCode = fragmentsOpCode;

                // Handle fragmented non-control frames
                byte[] payload = null;
                if (!frame.Header.IsControl) {
                    if (!frame.Header.FinalFragment) {
                        // We haven't received the entire message yet
                        fragmentsPayload.Append (frame.Payload, 0, frame.Payload.Length);
                    } else if (fragmentsPayload.Length > 0) {
                        // Payload for the entire message from the fragments
                        fragmentsPayload.Append (frame.Payload, 0, frame.Payload.Length);
                        payload = fragmentsPayload.ToArray ();
                        fragmentsPayload.Length = 0;
                        fragmentsVerifiedPosition = 0;
                    } else {
                        // Unfragmented message
                        payload = frame.Payload;
                    }
                }

                // Handle the frame
                if (opCode == OpCode.Binary) {
                    // Process binary frame
                    if (frame.Header.FinalFragment) {
                        if (shouldEcho)
                            Stream.Write (Frame.Binary (payload).ToBytes ());
                        else {
                            try {
                                request = Schema.KRPC.Request.Parser.ParseFrom (payload).ToMessage ();
                            } catch (InvalidProtocolBufferException) {
                                Logger.WriteLine ("WebSockets invalid message: failed to decode protobuf message", Logger.Severity.Error);
                                Stream.Write (Frame.Close (1007, "Malformed protocol buffer message").ToBytes ());
                                Stream.Close ();
                            }
                        }
                    }
                } else if (opCode == OpCode.Text) {
                    // Process text frame
                    if (frame.Header.FinalFragment) {
                        if (!Text.IsValidUTF8 (payload, 0, payload.Length)) {
                            Logger.WriteLine ("WebSockets invalid message: malformed UTF8 string", Logger.Severity.Error);
                            Stream.Write (Frame.Close (1007, "Malformed UTF8 string").ToBytes ());
                            Stream.Close ();
                        } else {
                            if (shouldEcho) {
                                Stream.Write (new Frame (OpCode.Text, payload).ToBytes ());
                            } else {
                                Logger.WriteLine ("WebSockets invalid message: text frames are not permitted", Logger.Severity.Error);
                                Stream.Write (Frame.Close (1003, "Text frames are not permitted").ToBytes ());
                                Stream.Close ();
                            }
                        }
                    } else if (fragmentsPayload.Length > 0) {
                        int truncatedCharLength = 0;
                        if (!Text.IsValidTruncatedUTF8 (fragmentsPayload.GetBuffer (), fragmentsVerifiedPosition, fragmentsPayload.Length - fragmentsVerifiedPosition, ref truncatedCharLength)) {
                            Logger.WriteLine ("WebSockets invalid message: malformed UTF8 string", Logger.Severity.Error);
                            Stream.Write (Frame.Close (1007, "Malformed UTF8 string").ToBytes ());
                            Stream.Close ();
                        }
                        fragmentsVerifiedPosition = fragmentsPayload.Length - truncatedCharLength;
                    }
                } else if (opCode == OpCode.Ping) {
                    // Send pong with copy of ping's payload
                    Stream.Write (Frame.Pong (frame.Payload).ToBytes ());
                } else if (opCode == OpCode.Close) {
                    if (frame.Header.Length >= 2) {
                        // Get status code from frame
                        var status = BitConverter.ToUInt16 (new [] { frame.Payload [1], frame.Payload [0] }, 0);
                        if (status < 1000 || status == 1004 || status == 1005 || status == 1006 || status == 1014 || (1015 <= status && status <= 2999)) {
                            // Send close if status code is invalid
                            Logger.WriteLine ("WebSockets invalid message: invalid close status code", Logger.Severity.Error);
                            Stream.Write (Frame.Close (1002).ToBytes ());
                        } else if (status >= 5000) {
                            // Close connection if undefined status is used
                        } else {
                            if (frame.Header.Length > 2) {
                                // Validate the contents as UTF8
                                if (!Text.IsValidUTF8 (frame.Payload, 2, (int)frame.Header.Length - 2)) {
                                    Logger.WriteLine ("WebSockets invalid message: malformed UTF8 string", Logger.Severity.Error);
                                    Stream.Write (Frame.Close (1002).ToBytes ());
                                    Stream.Close ();
                                    break;
                                }
                            }
                            // Send close with copy of status and optional message
                            Stream.Write (Frame.Close (frame.Payload).ToBytes ());
                        }
                    } else {
                        // Send close with no status
                        Stream.Write (Frame.Close ().ToBytes ());
                    }
                    Stream.Close ();
                }

                read += frame.Length;
                offset += frame.Length;
                length -= frame.Length;
                if (request != null)
                    return read;
            }
            return read;
        }
    }
}
