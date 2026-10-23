using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace KRPC.Server.ProtocolBuffers
{
    /// <summary>
    /// Encodes as a Message with one case set, without copying the case's content into a
    /// Message object.
    /// </summary>
    sealed class MessageEnvelope : IMessage
    {
        int field;
        IMessage content;

        public MessageDescriptor Descriptor { get { return null; } }

        public MessageEnvelope Set (int fieldNumber, IMessage value)
        {
            field = fieldNumber;
            content = value;
            return this;
        }

        public int CalculateSize ()
        {
            int size = content.CalculateSize ();
            return CodedOutputStream.ComputeTagSize (field) + CodedOutputStream.ComputeLengthSize (size) + size;
        }

        public void WriteTo (CodedOutputStream output)
        {
            output.WriteTag (field, WireFormat.WireType.LengthDelimited);
            output.WriteMessage (content);
        }

        public void MergeFrom (CodedInputStream input)
        {
            throw new System.NotImplementedException ();
        }
    }
}
