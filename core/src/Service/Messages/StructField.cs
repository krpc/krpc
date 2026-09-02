namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StructField : IMessage
    {
        public string Name { get; private set; }

        public TypeSpec Type { get; private set; }

        public string Documentation { get; set; }

        public bool Deprecated { get; set; }

        public string DeprecatedReason { get; set; }

        public StructField (string name, TypeSpec type)
        {
            Name = name;
            Type = type;
            Documentation = string.Empty;
            DeprecatedReason = string.Empty;
        }
    }
}
