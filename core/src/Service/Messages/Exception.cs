
namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Exception : IMessage
    {
        public string Name { get; private set; }

        public uint Id { get; private set; }

        public string Documentation { get; set; }

        public bool Deprecated { get; set; }

        public string DeprecatedReason { get; set; }

        public string MappedType { get; set; }

        public Exception (string name, uint id)
        {
            Name = name;
            Id = id;
            Documentation = string.Empty;
            DeprecatedReason = string.Empty;
            MappedType = string.Empty;
        }
    }
}
