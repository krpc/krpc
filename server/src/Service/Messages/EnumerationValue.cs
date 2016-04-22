using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class EnumerationValue : IMessage
    {
        public string Name = "";
        public int Value;
        public string Documentation = "";
    }
}
