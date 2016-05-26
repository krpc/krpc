using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    class StreamRequest
    {
        static uint nextIdentifier = 0;

        public uint Identifier { get; private set; }

        public ProcedureSignature Procedure { get; private set; }

        public object[] Arguments { get; private set; }

        public StreamResponse Response { get; private set; }

        public StreamRequest (Request request)
        {
            Identifier = nextIdentifier;
            nextIdentifier++;
            Procedure = Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            Arguments = Services.Instance.GetArguments (Procedure, request.Arguments);
            Response = new StreamResponse ();
            Response.Id = Identifier;
        }
    }
}
