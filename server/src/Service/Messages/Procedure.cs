using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Procedure : IMessage
    {
        public string Name = "";
        public IList<Parameter> Parameters = new List<Parameter> ();
        public bool HasReturnType = false;
        public string ReturnType = "";
        public IList<string> Attributes = new List<string> ();
        public string Documentation = "";
    }
}
