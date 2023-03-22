using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a procedure call.
    /// </summary>
    sealed class ProcedureCallContinuation /*: Continuation<ProcedureResult>*/
    {
        readonly ProcedureCall call;
        readonly ProcedureSignature procedure;
        readonly System.Exception exception;
        readonly Func<object> continuation;

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public ProcedureCallContinuation (ProcedureCall procedureCall)
        {
            call = procedureCall;
            try {
                procedure = Services.Instance.GetProcedureSignature (call);
            } catch (RPCException e) {
                exception = e;
            } catch (System.Exception e) {
                exception = e;
            }
        }

        ProcedureCallContinuation (ProcedureSignature invokedProcedure, Func<object> currentContinuation)
        {
            procedure = invokedProcedure;
            continuation = currentContinuation;
        }

        public ProcedureResult Run ()
        {
            var services = Services.Instance;
            if (exception != null)
              return new ProcedureResult { Error = services.HandleException (exception) };
            try {
                if (continuation == null)
                    return services.ExecuteCall(procedure, call);
                return services.ExecuteCall(procedure, continuation);
            }
            catch (YieldException e)
            {
                throw new YieldException<ProcedureCallContinuation>(new ProcedureCallContinuation(procedure, () => e.CallUntyped()));
            }
        }

        public ProcedureSignature Procedure {
            get { return procedure; }
        }
    };
}
