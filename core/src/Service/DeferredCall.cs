using System;
using KRPC.Server;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// A call that a server side function started and detached from itself, by
    /// pausing execution. The core runs the continuation on each update until the
    /// call completes. Nothing waits for the result, so a failure is logged, and the
    /// client that started the call owns it.
    /// </summary>
    sealed class DeferredCall
    {
        readonly ProcedureSignature procedure;
        Func<object> continuation;

        /// <summary>
        /// The client whose call or stream started the call. Null if the function was
        /// evaluated outside a request.
        /// </summary>
        public IClient Client { get; private set; }

        public DeferredCall (ProcedureSignature calledProcedure, YieldException yielded, IClient client)
        {
            if (ReferenceEquals (yielded, null))
                throw new ArgumentNullException (nameof (yielded));
            procedure = calledProcedure;
            continuation = () => yielded.CallUntyped ();
            Client = client;
        }

        /// <summary>
        /// Run the call, and return whether it completed.
        /// </summary>
        public bool Run ()
        {
            try {
                continuation ();
            } catch (YieldException e) {
                continuation = () => e.CallUntyped ();
                return false;
            } catch (System.Exception e) {
                Logger.WriteLine (
                    "Deferred call to " + procedure.FullyQualifiedName +
                    " failed. " + e.Message, Logger.Severity.Error);
            }
            return true;
        }

        /// <summary>
        /// Stop running the call, giving the reason. The procedures have no undo, so
        /// whatever the call has done so far stands.
        /// </summary>
        public void Cancel (string reason)
        {
            Logger.WriteLine (
                "Deferred call to " + procedure.FullyQualifiedName +
                " canceled part way through. " + reason, Logger.Severity.Info);
        }
    }
}
