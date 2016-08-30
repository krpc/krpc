using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    sealed class StreamRequest
    {
        public ulong Identifier { get; private set; }

        public ProcedureSignature Procedure { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public object[] Arguments { get; private set; }

        public StreamResult Result { get; private set; }

        public StreamRequest (Request request)
        {
            Identifier = NextIdentifier;
            var services = Services.Instance;
            Procedure = services.GetProcedureSignature (request.Service, request.Procedure);
            Arguments = services.GetArguments (Procedure, request.Arguments);
            Result = new StreamResult (Identifier);
        }

        static ulong nextIdentifier;

        static ulong NextIdentifier {
            get {
                var result = nextIdentifier;
                nextIdentifier++;
                return result;
            }
        }
    }
}
