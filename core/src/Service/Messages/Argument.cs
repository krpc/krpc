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
    }
}
