using System;
using System.IO;
using System.Linq;
using Google.Protobuf;
using KRPC.Server.Message;
using KRPC.Utils;
using SchemaMessage = KRPC.Schema.KRPC.Message;

namespace KRPC.Server.WebSockets
{
    /// <summary>
    /// A websockets connection. It starts with an HTTP upgrade, then carries one message
    /// in each binary websockets message.
    /// </summary>
    sealed class Connection : Message.Connection
    {
        /// <summary>
        /// Whether the connection should just echo messages back to the client.
        /// Used for running the Autobahn tests.
        /// </summary>
        readonly bool shouldEcho;

        /// <summary>
        /// Whether the HTTP upgrade has completed.
        /// </summary>
        bool upgraded;

        /// <summary>
        /// Whether a close frame has been written.
        /// </summary>
        bool closeSent;

        /// <summary>
        /// Whether a close frame has been received from the client.
        /// </summary>
        bool closeReceived;

        /// <summary>
        /// Payload bytes of a partly received frame still to be discarded while draining.
        /// </summary>
        long discardRemaining;

        /// <summary>
        /// Op code for the fragments.
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

        public Connection (Message.Server server, IClient<byte,byte> client, bool echo) : base (server, client)
        {
            shouldEcho = echo;
        }

        protected override bool PartialMessage {
            get { return fragmentsOpCode != OpCode.Close; }
        }

        protected override double StallTimeout {
            get { return shouldEcho ? double.PositiveInfinity : base.StallTimeout; }
        }

        protected override bool TimesHandshake {
            get { return !shouldEcho; }
        }

        protected override bool WriteClosed {
            get { return closeSent; }
        }

        protected override void Write (IMessage message)
        {
            // No frame before the upgrade, where the client reads HTTP, or after a close frame
            if (!upgraded || closeSent)
                return;
            using (var bufferStream = new MemoryStream ()) {
                message.WriteTo (bufferStream);
                var payload = bufferStream.ToArray ();
                var frame = new Frame (OpCode.Binary, payload);
                Stream.Write (frame.Header.ToBytes ());
                Stream.Write (frame.Payload);
            }
        }

        protected override void HalfClose ()
        {
            if (upgraded) {
                WriteClose (Frame.Close (1000));
            } else {
                var stream = Stream as IHalfClosableStream;
                if (stream != null)
                    stream.HalfClose ();
            }
        }

        void WriteClose (Frame closeFrame)
        {
            if (closeSent)
                return;
            closeSent = true;
            Stream.Write (closeFrame.ToBytes ());
        }

        /// <summary>
        /// End the connection after writing a close frame.
        /// </summary>
        ClientDisconnectedException Fail (Frame closeFrame, string reason)
        {
            Logger.WriteLine ("Invalid WebSockets message from " + Label + ": " + reason, Logger.Severity.Warning);
            WriteClose (closeFrame);
            return new ClientDisconnectedException (reason);
        }

        /// <summary>
        /// Skip the frames received while draining. The client's close frame, received before
        /// or after ours, completes the close.
        /// </summary>
        protected override int Discard (byte[] data, int length, out bool closed)
        {
            closed = closeReceived;
            if (!upgraded || closed)
                return length;
            var offset = 0;
            while (offset < length) {
                if (discardRemaining > 0) {
                    var skip = (int)Math.Min (discardRemaining, length - offset);
                    offset += skip;
                    discardRemaining -= skip;
                    continue;
                }
                Frame frame;
                try {
                    frame = Frame.FromBytes (data, offset, length - offset);
                } catch (FramingException) {
                    // The frame boundaries are lost, so we skip the bytes until the client closes
                    return length;
                } catch (NoRequestException) {
                    break;
                }
                if (frame.Header.OpCode == OpCode.Close) {
                    closed = true;
                    return length;
                }
                offset += frame.Length;
                if (frame.IsPartial)
                    discardRemaining = (long)frame.Header.Length - frame.Payload.Length;
            }
            return offset;
        }

        int Upgrade (byte[] data, int offset, int length)
        {
            var requestLength = ConnectionRequest.Length (data, offset, length);
            if (requestLength == 0)
                return 0;
            try {
                var request = ConnectionRequest.Read (data, offset, requestLength);
                Stream.Write (ConnectionRequest.WriteResponse (request.Headers ["sec-websocket-key"].Single ()));
            } catch (HandshakeException e) {
                Logger.WriteLine ("WebSockets handshake from " + Label + " failed: " + e.Response, Logger.Severity.Warning);
                Stream.Write (e.Response.ToBytes ());
                throw new ClientDisconnectedException ("WebSockets handshake failed");
            }
            upgraded = true;
            return requestLength;
        }

        protected override int Read (ref SchemaMessage message, byte[] data, int offset, int length)
        {
            if (!upgraded)
                return Upgrade (data, offset, length);
            int read = 0;
            while (length > 0) {
                // Read the next frame
                Frame frame;
                try {
                    frame = Frame.FromBytes (data, offset, length);
                } catch (FramingException e) {
                    throw Fail (Frame.Close (e.Status, e.Message), e.Message);
                } catch (NoRequestException) {
                    return read;
                }

                if (frame.Header.Rsv1 || frame.Header.Rsv2 || frame.Header.Rsv3)
                    throw Fail (Frame.Close (1002), "RSV bit(s) set in frame header");
                if (frame.Header.OpCode == OpCode.Continue && fragmentsOpCode == OpCode.Close)
                    throw Fail (Frame.Close (1002), "message fragment received out of order");
                if (frame.Header.OpCode != OpCode.Continue && !frame.Header.IsControl && fragmentsOpCode != OpCode.Close)
                    throw Fail (Frame.Close (1002), "message start fragment received out of order");

                if (!frame.Header.IsControl && fragmentsPayload.Length > int.MaxValue - frame.Payload.Length)
                    throw Fail (Frame.Close (1009, "Message is too large"), "message is too large");

                // Partial payload received
                if (frame.IsPartial) {
                    // Check that partially received text frames are valid UTF8
                    if (frame.Header.OpCode == OpCode.Text || (frame.Header.OpCode == OpCode.Continue && fragmentsOpCode == OpCode.Text)) {
                        fragmentsPayload.Append (frame.Payload, 0, frame.Payload.Length);
                        int truncatedCharLength = 0;
                        if (!Text.IsValidTruncatedUTF8 (fragmentsPayload.GetBuffer (), fragmentsVerifiedPosition, fragmentsPayload.Length - fragmentsVerifiedPosition, ref truncatedCharLength))
                            throw Fail (Frame.Close (1007, "Malformed UTF8 string"), "malformed UTF8 string");
                        fragmentsPayload.Length -= frame.Payload.Length;
                    }
                    return read;
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
                    if (frame.Header.FinalFragment)
                        fragmentsOpCode = OpCode.Close;
                }

                read += frame.Length;
                offset += frame.Length;
                length -= frame.Length;

                // Handle the frame
                if (opCode == OpCode.Binary) {
                    if (frame.Header.FinalFragment) {
                        if (shouldEcho)
                            Stream.Write (Frame.Binary (payload).ToBytes ());
                        else {
                            try {
                                message = SchemaMessage.Parser.ParseFrom (payload);
                            } catch (InvalidProtocolBufferException e) {
                                throw new MalformedRequestException (e.Message, read);
                            }
                            return read;
                        }
                    }
                } else if (opCode == OpCode.Text) {
                    if (frame.Header.FinalFragment) {
                        if (!Text.IsValidUTF8 (payload, 0, payload.Length))
                            throw Fail (Frame.Close (1007, "Malformed UTF8 string"), "malformed UTF8 string");
                        if (!shouldEcho)
                            throw Fail (Frame.Close (1003, "Text frames are not permitted"), "text frames are not permitted");
                        Stream.Write (new Frame (OpCode.Text, payload).ToBytes ());
                    } else if (fragmentsPayload.Length > 0) {
                        int truncatedCharLength = 0;
                        if (!Text.IsValidTruncatedUTF8 (fragmentsPayload.GetBuffer (), fragmentsVerifiedPosition, fragmentsPayload.Length - fragmentsVerifiedPosition, ref truncatedCharLength))
                            throw Fail (Frame.Close (1007, "Malformed UTF8 string"), "malformed UTF8 string");
                        fragmentsVerifiedPosition = fragmentsPayload.Length - truncatedCharLength;
                    }
                } else if (opCode == OpCode.Ping) {
                    // Send pong with copy of ping's payload
                    Stream.Write (Frame.Pong (frame.Payload).ToBytes ());
                } else if (opCode == OpCode.Close) {
                    throw Closed (frame);
                }
            }
            return read;
        }

        /// <summary>
        /// Answer a close frame from the client, then end the connection.
        /// </summary>
        ClientDisconnectedException Closed (Frame frame)
        {
            closeReceived = true;
            if (closeSent)
                return new ClientDisconnectedException ("WebSockets connection closed by the client");
            if (frame.Header.Length >= 2) {
                // Get status code from frame
                var status = BitConverter.ToUInt16 (new [] { frame.Payload [1], frame.Payload [0] }, 0);
                if (status < 1000 || status == 1004 || status == 1005 || status == 1006 || status == 1014 || (1015 <= status && status <= 2999))
                    return Fail (Frame.Close (1002), "invalid close status code");
                if (status < 5000) {
                    // Validate the contents as UTF8
                    if (frame.Header.Length > 2 && !Text.IsValidUTF8 (frame.Payload, 2, (int)frame.Header.Length - 2))
                        return Fail (Frame.Close (1002), "malformed UTF8 string");
                    // Send close with copy of status and optional message
                    WriteClose (Frame.Close (frame.Payload));
                }
                // Close connection with no reply if an undefined status is used
                closeSent = true;
            } else {
                // Send close with no status
                WriteClose (Frame.Close ());
            }
            return new ClientDisconnectedException ("WebSockets connection closed by the client");
        }
    }
}
