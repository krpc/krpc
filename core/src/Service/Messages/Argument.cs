using Google.Protobuf;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Argument : IMessage
    {
        public uint Position { get; private set; }

        public object Value { get; private set; }

        public Argument (uint position, object value)
        {
            Position = position;
            Value = value;
        }

        /// <summary>
        /// An argument left encoded, to be decoded on the game thread when its call runs.
        /// </summary>
        internal Argument (uint position, ByteString encoded, TypeSpec spec)
        {
            Position = position;
            Encoded = encoded;
            Spec = spec;
        }

        /// <summary>
        /// The encoded value, or null once the value is decoded.
        /// </summary>
        internal ByteString Encoded { get; private set; }

        /// <summary>
        /// The type the encoded value decodes as.
        /// </summary>
        internal TypeSpec Spec { get; private set; }

        internal void Decoded (object value)
        {
            Value = value;
            Encoded = null;
        }
    }
}
