using KRPC.Schema.KRPC;
using KRPC.Service.Scanner;

namespace KRPC.Server.Stream
{
    class StreamRequest
    {
        static uint nextIdentifier = 0;

        public uint Identifier { get; private set; }

        public ProcedureSignature Procedure { get; private set; }

        public object[] Arguments { get; private set; }

        public StreamResponse.Builder ResponseBuilder { get; private set; }

        public StreamRequest (Request request)
        {
            Identifier = nextIdentifier;
            nextIdentifier++;
            Procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request);
            Arguments = KRPC.Service.Services.Instance.DecodeArguments (Procedure, request);
            ResponseBuilder = StreamResponse.CreateBuilder ();
            ResponseBuilder.SetId (Identifier);
        }
    }
}

