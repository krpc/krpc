using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Struct : IMessage
    {
        public string Name { get; private set; }

        public uint Id { get; private set; }

        public IList<StructField> Fields { get; private set; }

        public string Documentation { get; set; }

        public bool Deprecated { get; set; }

        public string DeprecatedReason { get; set; }

        public Struct (string name, uint id)
        {
            Name = name;
            Id = id;
            Fields = new List<StructField> ();
            Documentation = string.Empty;
            DeprecatedReason = string.Empty;
        }
    }
}
