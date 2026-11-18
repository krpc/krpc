using System;
using Google.Protobuf;
using KRPC.Server.Message;
using SchemaMessage = KRPC.Schema.KRPC.Message;

namespace KRPC.Server.Delimited
{
    /// <summary>
    /// A connection whose messages are each prefixed by their length, as a varint.
    /// </summary>
    class Connection : Message.Connection
    {
        readonly CodedOutputStream codedOutputStream;

        public Connection (Message.Server server, IClient<IByteStream> client) : base (server, client)
        {
            codedOutputStream = new CodedOutputStream (Output, true);
        }

        protected override int Read (ref SchemaMessage message, byte[] data, int offset, int length)
        {
            int prefixLength;
            int size;
            try {
                size = DecodeMessageLength (data, offset, length, out prefixLength);
            } catch (System.InvalidOperationException e) {
                throw new MalformedRequestException (e.Message, System.Math.Min (length, 5));
            }
            if (prefixLength == 0)
                return 0;
            if (size > int.MaxValue - prefixLength)
                throw new MalformedRequestException ("Message length prefix is out of range", length);
            if (size > length - prefixLength)
                return 0;
            try {
                message = SchemaMessage.Parser.ParseFrom (data, offset + prefixLength, size);
            } catch (InvalidProtocolBufferException e) {
                throw new MalformedRequestException (e.Message, prefixLength + size);
            }
            return prefixLength + size;
        }

        protected override void Write (IMessage message)
        {
            codedOutputStream.WriteMessage (message);
            codedOutputStream.Flush ();
        }

        protected override void HalfClose ()
        {
            var stream = Stream as IHalfClosableStream;
            if (stream != null)
                stream.HalfClose ();
        }

        /// <summary>
        /// Decode the varint length prefix at the start of the data, and set
        /// <paramref name="prefixLength"/> to the bytes it takes. Sets it to zero if the data
        /// holds only part of the prefix.
        /// </summary>
        static int DecodeMessageLength (byte[] data, int offset, int length, out int prefixLength)
        {
            var result = 0;
            // A varint encoding a 32-bit value is at most five bytes long.
            for (var i = 0; i < 5; i++) {
                if (i >= length) {
                    prefixLength = 0;
                    return 0;
                }
                int b = data [offset + i];
                // The fifth byte holds bits 28 to 34, of which only 28 to 30 fit a positive int
                if (i == 4 && b > 0x07)
                    throw new InvalidOperationException ("Message length prefix is out of range");
                result |= (b & 0x7f) << (7 * i);
                if ((b & 0x80) == 0) {
                    prefixLength = i + 1;
                    return result;
                }
            }
            throw new InvalidOperationException ("Message length prefix is not a valid varint");
        }
    }
}
