using Google.Protobuf;
using KRPC.Server.Message;
using SchemaMessage = KRPC.Schema.KRPC.Message;

namespace KRPC.Server.ProtocolBuffers
{
    /// <summary>
    /// A connection whose messages are each prefixed by their length, as a varint.
    /// </summary>
    class Connection : Message.Connection
    {
        readonly CodedOutputStream codedOutputStream;

        public Connection (Message.Server server, IClient<byte,byte> client) : base (server, client)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (client.Stream), true);
        }

        protected override int Read (ref SchemaMessage message, byte[] data, int offset, int length)
        {
            int prefixLength;
            int size;
            try {
                size = Utils.DecodeMessageLength (data, offset, length, out prefixLength);
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
    }
}
