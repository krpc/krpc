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
        public StreamRequest Request { get; private set; }

        ProcedureCallContinuation originalCall;
        ProcedureCallContinuation call;

        /// <summary>
        /// Create a stream continuation used to execute a stream RPC
        /// </summary>
        public StreamContinuation (StreamRequest request)
        {
            Request = request;
            call = new ProcedureCallContinuation (request.Call);
            originalCall = call;
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
            if (call == null)
                throw new InvalidOperationException (
                    "The stream continuation threw an exception previously and cannot be re-run");
            try {
                var result = call.Run ();
                call = originalCall;
                return result;
            } catch (YieldException e) {
                call = (ProcedureCallContinuation)e.Continuation;
                throw new YieldException (this);
            } catch (System.Exception) {
                call = null;
                throw;
            }
        }
    }
}
