using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class StreamRequest
    {
        public uint Identifier { get; private set; }

        public ProcedureSignature Procedure { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public object[] Arguments { get; private set; }

        public StreamResponse Response { get; private set; }

        public StreamRequest (Request request)
        {
            Identifier = NextIdentifier;
            var services = Services.Instance;
            Procedure = services.GetProcedureSignature (request.Service, request.Procedure);
            Arguments = services.GetArguments (Procedure, request.Arguments);
            Response = new StreamResponse (Identifier);
        }

        static uint nextIdentifier;

        static uint NextIdentifier {
            get {
                var result = nextIdentifier;
                nextIdentifier++;
                return result;
            }
        }
    }
}
