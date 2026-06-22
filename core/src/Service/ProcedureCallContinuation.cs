using System;
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
        readonly System.Exception exception;
        readonly Func<object> continuation;

        public ProcedureCallContinuation (ProcedureCall procedureCall)
        {
            call = procedureCall;
            try {
                Procedure = procedureCall.CachedSignature ?? Services.Instance.GetProcedureSignature (call);
            } catch (RPCException e) {
                exception = e;
            } catch (System.Exception e) {
                exception = e;
            }
        }

        ProcedureCallContinuation (ProcedureSignature invokedProcedure, Func<object> currentContinuation)
        {
            Procedure = invokedProcedure;
            continuation = currentContinuation;
        }

        public ProcedureResult Run ()
        {
            var services = Services.Instance;
            if (exception != null)
              return new ProcedureResult { Error = services.HandleException (exception) };
            try {
                if (continuation == null)
                    return services.ExecuteCall(Procedure, call);
                return services.ExecuteCall(Procedure, continuation);
            }
            catch (YieldException e)
            {
                throw new YieldException<ProcedureCallContinuation>(new ProcedureCallContinuation(Procedure, () => e.CallUntyped()));
            }
        }

        /// <summary>
        /// Like Run() but writes the result into an existing ProcedureResult instead of
        /// allocating a new one. Throws YieldException if the call yields (result is unchanged).
        /// </summary>
        public void RunInto (ProcedureResult result)
        {
            var services = Services.Instance;
            if (exception != null) {
                result.Reset ();
                result.Error = services.HandleException (exception);
                return;
            }
            try {
                if (continuation == null)
                    services.ExecuteCallInto (Procedure, call, result);
                else
                    services.ExecuteCallInto (Procedure, continuation, result);
            } catch (YieldException e) {
                throw new YieldException<ProcedureCallContinuation> (new ProcedureCallContinuation (Procedure, () => e.CallUntyped ()));
            }
        }

        public ProcedureSignature Procedure {
            get; private set;
        }
    };
}
