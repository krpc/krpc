namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class EnumerationValue : IMessage
    {
        public string Name { get; private set; }

        public int Value { get; private set; }

        public string Documentation { get; set; }

        public EnumerationValue (string name, int value)
        {
            Name = name;
            Value = value;
            Documentation = string.Empty;
        }
    }
}
