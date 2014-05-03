using KRPC.Schema.KRPC;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a client request.
    /// </summary>
    class RequestContinuation : Continuation<Response.Builder>
    {
        public IClient Client { get; private set; }

        Request request;
        ProcedureSignature procedure;
        IContinuation continuation;

        public RequestContinuation (IClient client, Request request)
        {
            Client = client;
            this.request = request;
            this.procedure = Services.Instance.GetProcedureSignature (request);
        }

        RequestContinuation (IClient client, Request request, ProcedureSignature procedure, IContinuation continuation)
        {
            Client = client;
            this.request = request;
            this.procedure = procedure;
            this.continuation = continuation;
        }

        public override Response.Builder Run ()
        {
            try {
                if (continuation == null)
                    return Services.Instance.HandleRequest (procedure, request);
                else
                    return Services.Instance.HandleRequest (procedure, continuation);
            } catch (YieldException e) {
                throw new YieldException (new RequestContinuation (Client, request, procedure, e.Continuation));
            }
        }
    };
}
