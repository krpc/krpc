using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Messages;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a stream.
    /// </summary>
    sealed class StreamContinuation : Continuation<ProcedureResult>
    {
        ProcedureCallContinuation originalContinuation;
        ProcedureCallContinuation continuation;

        /// <summary>
        /// Create a stream continuation used to execute a stream RPC
        /// </summary>
        public StreamContinuation (ProcedureCall call)
        {
            continuation = new ProcedureCallContinuation (call);
            originalContinuation = continuation;
        }

        /// <summary>
        /// Execute the procedure call for the stream request.
        /// Throws a YieldException if the procedure calls yield.
        /// Calling this method again will then re-execute the procedure call that yielded.
        /// If the procedure completes a return value is returned, and the continuation is reset
        /// such that it can be called again.
        /// If the procedure throws an exception, the continuation should not be run
        /// again (the stream should be removed from the server).
        /// </summary>
        public override ProcedureResult Run ()
        {
            if (continuation == null)
                throw new InvalidOperationException (
                    "The stream continuation threw an exception previously and cannot be re-run");
            try {
                var result = continuation.Run ();
                continuation = originalContinuation;
                return result;
            } catch (YieldException e) {
                continuation = (ProcedureCallContinuation)e.Continuation;
                throw new YieldException (this);
            } catch (System.Exception) {
                continuation = null;
                throw;
            }
        }
    }
}
