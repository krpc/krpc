using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Enumeration : IMessage
    {
        public string Name { get; private set; }

        public IList<EnumerationValue> Values { get; private set; }

        public string Documentation { get; set; }

        public Enumeration (string name)
        {
            Name = name;
            Values = new List<EnumerationValue> ();
            Documentation = string.Empty;
        }
    }
}
