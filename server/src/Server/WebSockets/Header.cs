using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server.Message;

namespace KRPC.Server.WebSockets
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class Header
    {
        public bool FinalFragment { get; set; }

        public bool Rsv1 { get; set; }

        public bool Rsv2 { get; set; }

        public bool Rsv3 { get; set; }

        public OpCode OpCode { get; private set; }

        public bool IsControl {
            get {
                return
                OpCode == OpCode.Ping ||
                OpCode == OpCode.Pong ||
                OpCode == OpCode.Close;
            }
        }

        public bool Masked { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] MaskingKey {
            get { return maskingKey; }
            set {
                if (value != null && value.Length != 4)
                    throw new ArgumentException ("Masking key must be 4 bytes");
                maskingKey = value;
                Masked = maskingKey != null;
            }
        }

        /// <summary>
        /// Length of the payload in bytes.
        /// </summary>
        public ulong Length { get; private set; }

        /// <summary>
        /// The length of the entire header in bytes. */
        /// </summary>
        public int HeaderLength {
            get {
                var length = 2;
                if (Length > 0xffff)
                    length += 8;
                else if (Length > 125)
                    length += 2;
                if (Masked)
                    length += 4;
                return length;
            }
        }

        byte[] maskingKey;

        const byte FINISH_MASK = 0x80;
        const byte RSV1_MASK = 0x40;
        const byte RSV2_MASK = 0x20;
        const byte RSV3_MASK = 0x10;
        const byte OP_CODE_MASK = 0x0F;
        const byte MASK_MASK = 0x80;
        const byte PAYLOAD_MASK = 0x7F;

        /// <summary>
        /// Create a header with the given op code and payload length.
        /// </summary>
        public Header (OpCode opCode, ulong length)
        {
            FinalFragment = true;
            OpCode = opCode;
            Length = length;
        }

        Header ()
        {
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public byte[] ToBytes ()
        {
            var bytes = new byte[HeaderLength];

            if (FinalFragment)
                bytes [0] |= FINISH_MASK;
            if (Rsv1)
                bytes [0] |= RSV1_MASK;
            if (Rsv2)
                bytes [0] |= RSV2_MASK;
            if (Rsv3)
                bytes [0] |= RSV3_MASK;
            bytes [0] |= (byte)OpCode;
            if (Masked)
                bytes [1] |= MASK_MASK;

            if (Length <= 125) {
                bytes [1] |= (byte)Length;
            } else if (Length <= 0xffff) {
                bytes [1] |= 126;
                byte[] size = BitConverter.GetBytes ((short)Length);
                bytes [2] = size [1];
                bytes [3] = size [0];
            } else {
                bytes [1] |= 127;
                byte[] size = BitConverter.GetBytes ((long)Length);
                for (int i = 0; i < 8; i++)
                    bytes [2 + i] = size [7 - i];
            }

            if (Masked)
                Array.Copy (MaskingKey, 0, bytes, bytes.Length - 4, 4);

            return bytes;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static Header FromBytes (byte[] data, int index, int length)
        {
            var header = new Header ();
            var headerLength = 2;
            if (length < headerLength)
                throw new NoRequestException ();

            byte firstByte = data [index];
            byte secondByte = data [index + 1];

            header.FinalFragment = ((firstByte & FINISH_MASK) != 0);
            header.Rsv1 = ((firstByte & RSV1_MASK) != 0);
            header.Rsv2 = ((firstByte & RSV2_MASK) != 0);
            header.Rsv3 = ((firstByte & RSV3_MASK) != 0);

            if (!Enum.IsDefined (typeof(OpCode), firstByte & OP_CODE_MASK))
                throw new FramingException (1002, "Invalid op code");
            header.OpCode = (OpCode)(firstByte & OP_CODE_MASK);

            var isControl = header.IsControl;
            if (!header.FinalFragment && isControl)
                throw new FramingException (1002, "Control frames must not be fragmented");

            var isMasked = ((secondByte & MASK_MASK) != 0);
            var payloadLength = (byte)(secondByte & PAYLOAD_MASK);
            var extPayloadLengthSize = 0;

            if (payloadLength == 126) {
                headerLength += 2;
                if (length < headerLength)
                    throw new NoRequestException ();
                var thirdByte = data [index + 2];
                var fourthByte = data [index + 3];
                byte[] lengthBytes = { fourthByte, thirdByte };
                header.Length = BitConverter.ToUInt16 (lengthBytes, 0);
                extPayloadLengthSize = 2;
            } else if (payloadLength == 127) {
                headerLength += 8;
                if (length < headerLength)
                    throw new NoRequestException ();
                var lengthBytes = new byte[8];
                for (int i = 0; i < 8; i++)
                    lengthBytes [i] = data [index + 9 - i];
                header.Length = BitConverter.ToUInt64 (lengthBytes, 0);
                extPayloadLengthSize = 8;
            } else
                header.Length = payloadLength;

            if (isControl && header.Length >= 126)
                throw new FramingException (1002, "Control frame payload must not exceed 125 bytes");

            if (isMasked) {
                headerLength += 4;
                if (length < headerLength)
                    throw new NoRequestException ();
                var mask = new byte[4];
                Array.Copy (data, index + 2 + extPayloadLengthSize, mask, 0, 4);
                header.MaskingKey = mask;
            }

            return header;
        }
    }
}
