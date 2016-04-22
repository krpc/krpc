using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Request : IMessage
    {
        public string Service = "";
        public string Procedure = "";
        public IList<Argument> Arguments = new List<Argument> ();
    }
}
