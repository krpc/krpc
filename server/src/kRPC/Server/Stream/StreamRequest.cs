using KRPC.Service.Scanner;
using Krpc;

namespace KRPC.Server.Stream
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
            Procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request);
            Arguments = KRPC.Service.Services.Instance.DecodeArguments (Procedure, request);
            Response = new StreamResponse ();
            Response.Id = Identifier;
        }
    }
}

