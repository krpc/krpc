using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Enumeration : IMessage
    {
        public string Name = "";
        public IList<EnumerationValue> Values = new List<EnumerationValue> ();
        public string Documentation = "";
    }
}
