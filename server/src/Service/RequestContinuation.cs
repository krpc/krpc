using System.Diagnostics.CodeAnalysis;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Messages;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a client request.
    /// </summary>
    sealed class RequestContinuation : Continuation<Response>
    {
        public IClient<Request,Response> Client { get; private set; }

        ProcedureCallContinuation call;
        readonly ProcedureCallContinuation[] calls;
        readonly ProcedureResult[] results;

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public RequestContinuation (IClient<Request,Response> client, Request request)
        {
            Client = client;
            int size = request.Calls.Count;
            if (size == 1) {
                // Special case for requests with a single call, to avoid allocating arrays
                call = new ProcedureCallContinuation (request.Calls [0]);
            } else {
                calls = new ProcedureCallContinuation[size];
                results = new ProcedureResult[size];
                for (int i = 0; i < size; i++)
                    calls [i] = new ProcedureCallContinuation (request.Calls [i]);
            }
        }

        /// <summary>
        /// Execute the procedure calls contained in the request.
        /// Throws a YieldException if any of the procedure calls yield. Calling this method again will
        /// then re-execute those procedure calls that yielded.
        /// If all of the procedures complete, with either a return value or an error,
        /// a response containing all of the return values and errors is returned.
        /// </summary>
        public override Response Run ()
        {
            if (call != null) {
                // Special case when the request contains a single call, as the logic is much simpler
                try {
                    var result = call.Run();
                    var response = new Response ();
                    response.Results.Add(result);
                    return response;
                } catch (YieldException e) {
                    call = (ProcedureCallContinuation)e.Continuation;
                    throw new YieldException (this);
                }
            } else {
                bool yielded = false;
                for (int i = 0; i < calls.Length; i++) {
                    if (results [i] == null) {
                        try {
                            results [i] = calls [i].Run ();
                        } catch (YieldException e) {
                            calls [i] = (ProcedureCallContinuation)e.Continuation;
                            yielded = true;
                        }
                    }
                }
                if (yielded)
                    throw new YieldException (this);
                var response = new Response ();
                for (int i = 0; i < results.Length; i++)
                    response.Results.Add (results [i]);
                return response;
            }
        }
    }
}
