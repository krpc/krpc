using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a procedure call.
    /// </summary>
    sealed class ProcedureCallContinuation : Continuation<ProcedureResult>
    {
        readonly ProcedureCall call;
        readonly ProcedureSignature procedure;
        readonly Exception exception;
        readonly IContinuation continuation;

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public ProcedureCallContinuation (ProcedureCall procedureCall)
        {
            call = procedureCall;
            try {
                procedure = Services.Instance.GetProcedureSignature (call.Service, call.Procedure);
            } catch (Exception e) {
                exception = e;
            }
        }

        ProcedureCallContinuation (ProcedureSignature invokedProcedure, IContinuation currentContinuation)
        {
            procedure = invokedProcedure;
            continuation = currentContinuation;
        }

        public override ProcedureResult Run ()
        {
            if (exception != null)
                throw exception;
            try {
                var services = Services.Instance;
                if (continuation == null)
                    return services.ExecuteCall (procedure, call);
                else
                    return services.ExecuteCall (procedure, continuation);
            } catch (YieldException e) {
                throw new YieldException (new ProcedureCallContinuation (procedure, e.Continuation));
            }
        }
    };
}
