using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
    sealed class ProcedureCallStream : Stream
    {
        ProcedureSignature procedure;
        object[] arguments;
        StreamContinuation continuation;

        public ProcedureCallStream (ProcedureCall call)
        {
            var services = Services.Instance;
            procedure = services.GetProcedureSignature (call);
            if (!procedure.HasReturnType)
                throw new InvalidOperationException ("Cannot create a stream for a procedure that does not return a value.");
            arguments = services.GetArguments (procedure, call.Arguments);
            continuation = new StreamContinuation (call);
        }

        public override bool Equals (Stream other)
        {
            if (ReferenceEquals (other, null))
                return false;
            var obj = other as ProcedureCallStream;
            if (ReferenceEquals (obj, null))
                return false;
            return procedure == obj.procedure &&
                Enumerable.SequenceEqual (arguments, obj.arguments);
        }

        public override int GetHashCode ()
        {
            return procedure.GetHashCode ();
        }

        public override void UpdateInternal() {
            try  {
                Result = continuation.Run ();
            } catch (YieldException) {
                return;
            } catch (System.Exception e) {
                Result = new ProcedureResult {
                    Error = Service.Services.Instance.HandleException (e)
                };
            }
        }
    }
}
