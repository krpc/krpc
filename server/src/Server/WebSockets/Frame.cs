using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server.WebSockets
{
    sealed class Frame
    {
        public Header Header { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] Payload { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] MaskedPayload {
            get {
                if (!Header.Masked)
                    return Payload;
                var bytes = new byte[Payload.Length];
                Array.Copy (Payload, bytes, Payload.Length);
                for (int i = 0; i < Payload.Length; i++)
                    bytes [i] = (byte)(bytes [i] ^ Header.MaskingKey [i % 4]);
                return bytes;
            }
        }

        /// <summary>
        /// True if the payload length is less than the length indicated in the header, i.e. we have received only part of the payload.
        /// </summary>
        public bool IsPartial { get; private set; }

        /// <summary>
        /// Lenghth of the frame (including header and payload) in bytes.
        /// </summary>
        public int Length {
            get { return Header.HeaderLength + Payload.Length; }
        }

        public Frame (OpCode opCode)
        {
            Header = new Header (opCode, 0);
            Payload = new byte[0];
        }

        public Frame (OpCode opCode, byte[] payload)
        {
            Header = new Header (opCode, (ulong)payload.Length);
            Payload = payload;
        }

        Frame ()
        {
        }

        public byte[] ToBytes ()
        {
            var header = Header.ToBytes ();
            if (Payload.Length == 0)
                return header;
            var bytes = new byte[header.Length + Payload.Length];
            Array.Copy (header, bytes, header.Length);
            Array.Copy (MaskedPayload, 0, bytes, header.Length, Payload.Length);
            return bytes;
        }

        public static Frame FromBytes (byte[] data, int index, int count)
        {
            var frame = new Frame ();
            frame.Header = Header.FromBytes (data, index, count);
            var headerLength = frame.Header.HeaderLength;
            index += headerLength;
            count -= headerLength;
            if (frame.Header.Length == 0)
                frame.Payload = new byte[0];
            else {
                // See if the payload has been partially received
                frame.IsPartial = (count < (int)frame.Header.Length);
                // Payload must be masked
                if (!frame.Header.Masked)
                    throw new FramingException (1002, "Payload is not masked");
                // Unmask the payload
                frame.Payload = new byte [count < (int)frame.Header.Length ? count : (int)frame.Header.Length];
                for (int i = 0; i < frame.Payload.Length; i++)
                    frame.Payload [i] = (byte)(data [index + i] ^ frame.Header.MaskingKey [i % 4]);
            }
            return frame;
        }

        /// <summary>
        /// A close frame with no payload.
        /// </summary>
        public static Frame Close ()
        {
            return new Frame (OpCode.Close);
        }

        /// <summary>
        /// A close frame with a status and optional message.
        /// </summary>
        public static Frame Close (ushort status, string message = null)
        {
            if (message == null)
                message = string.Empty;
            var statusBytes = BitConverter.GetBytes (status);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes (message);
            var payload = new byte [statusBytes.Length + messageBytes.Length];
            payload [0] = statusBytes [1];
            payload [1] = statusBytes [0];
            Array.Copy (messageBytes, 0, payload, 2, messageBytes.Length);
            return new Frame (OpCode.Close, payload);
        }

        /// <summary>
        /// A close frame with the status from another close frames payload.
        /// </summary>
        public static Frame Close (byte[] payload)
        {
            return new Frame (OpCode.Close, new [] { payload [0], payload [1] });
        }

        /// <summary>
        /// A pong frame with the payload from a ping frame.
        /// </summary>
        public static Frame Pong (byte[] payload)
        {
            return new Frame (OpCode.Pong, payload);
        }

        /// <summary>
        /// A binary frame with a payload and an optional masking key.
        /// </summary>
        public static Frame Binary (byte[] payload, byte[] maskingKey = null)
        {
            var frame = new Frame (OpCode.Binary, payload);
            if (maskingKey != null)
                frame.Header.MaskingKey = maskingKey;
            return frame;
        }
    }
}
