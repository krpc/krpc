using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Request : IMessage
    {
        public string Service { get; private set; }

        public string Procedure { get; private set; }

        public IList<Argument> Arguments { get; private set; }

        public Request (string service, string procedure)
        {
            Service = service;
            Procedure = procedure;
            Arguments = new List<Argument> ();
        }
    }
}
