using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Request : IMessage
    {
        public IList<ProcedureCall> Calls { get; private set; }

        public Request ()
        {
            Calls = new List<ProcedureCall> ();
        }
    }
}
