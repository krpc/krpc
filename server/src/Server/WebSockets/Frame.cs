using System;

namespace KRPC.Server.WebSockets
{
    sealed class Frame
    {
        public Header Header { get; private set; }

        public byte[] Payload { get; private set; }

        /// <summary>
        /// Lenghth of the frame (including header and payload) in bytes.
        /// </summary>
        public int Length {
            get { return Header.HeaderLength + (Payload == null ? 0 : Payload.Length); }
        }

        public Frame (OpCode opCode)
        {
            Header = new Header (opCode, 0);
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
            if (Payload == null)
                return header;
            byte[] bytes = new byte[header.Length + Payload.Length];
            Array.Copy (header, bytes, header.Length);
            Array.Copy (Payload, 0, bytes, header.Length, Payload.Length);
            return bytes;
        }

        public static Frame FromBytes (byte[] data, int index, int count)
        {
            var frame = new Frame ();
            frame.Header = Header.FromBytes (data, index, count);
            index += frame.Header.HeaderLength;
            count -= frame.Header.HeaderLength;
            if ((int)frame.Header.Length < count)
                // Entire payload has not been received
                throw new NoRequestException ();
            // Payload must be masked
            if (!frame.Header.Masked)
                throw new FramingException ();
            // Unmask the payload
            //TODO: is there a more efficient way to do this?
            if (frame.Header.Length > 0) {
                frame.Payload = new byte[frame.Header.Length];
                for (int i = 0; i < frame.Payload.Length; i++)
                    frame.Payload [i] = (byte)(data [index + i] ^ frame.Header.MaskingKey [i % 4]);
            }
            return frame;
        }

        public static Frame Close (ushort status, string message)
        {
            var payload = BitConverter.GetBytes (status);
            //FIXME: add message
            return new Frame (OpCode.Close, payload);
        }
    }
}
