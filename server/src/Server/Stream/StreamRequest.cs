using KRPC.Service.Scanner;
using KRPC.Service.Messages;

namespace KRPC.Server.Stream
{
    class StreamRequest
    {
        static uint nextIdentifier = 0;

        public uint Identifier { get; private set; }

        public ProcedureSignature Procedure { get; private set; }

        public object[] Arguments { get; private set; }

        public StreamResponse Response { get; private set; }

        public StreamRequest (Service.Messages.Request request)
        {
            Identifier = nextIdentifier;
            nextIdentifier++;
            Procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            Arguments = KRPC.Service.Services.Instance.GetArguments (Procedure, request.Arguments);
            Response = new StreamResponse ();
            Response.Id = Identifier;
        }
    }
}

