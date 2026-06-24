using System;
using System.Linq;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
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
            var result = StreamResult.Result;
            bool wasSet = result.HasValue;
            object oldValue = result.Value;

            try {
                continuation.RunInto (result);
            } catch (YieldException) {
                return;
            } catch (System.Exception e) {
                result.Reset ();
                result.Error = Service.Services.Instance.HandleException (e);
                Changed = true;
                return;
            }

            if (result.HasValue) {
                if (!wasSet)
                    Changed = true;
                else if (!ReferenceEquals (result.Value, null))
                    Changed |= !ValueUtils.Equal (result.Value, oldValue);
                else
                    Changed |= !ReferenceEquals (oldValue, null);
            } else {
                Changed |= result.HasError;
            }
        }
    }
}
