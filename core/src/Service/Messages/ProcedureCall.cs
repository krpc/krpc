using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class ProcedureCall : IMessage
    {
        public string Service { get; private set; }

        public uint ServiceId { get; private set; }

        public string Procedure { get; private set; }

        public uint ProcedureId { get; private set; }

        public IList<Argument> Arguments { get; private set; }

        public ProcedureCall (string service, string procedure)
        {
            Service = service;
            ServiceId = 0;
            Procedure = procedure;
            ProcedureId = 0;
            Arguments = new List<Argument> ();
        }

        public ProcedureCall (string service, uint serviceId, string procedure, uint procedureId)
        {
            Service = service;
            ServiceId = serviceId;
            Procedure = procedure;
            ProcedureId = procedureId;
            Arguments = new List<Argument> ();
        }
    }
}
