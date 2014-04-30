using KRPC.Schema.KRPC;
using KRPC.Continuations;
using KRPC.Server;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a request object. Captures the common case where a
    /// request always returns a result, and never throws YieldException
    /// </summary>
    class RequestContinuation : Continuation<Response.Builder>
    {
        public IClient Client { get; private set; }

        public Request Request { get; private set; }

        IContinuation continuation;

        public RequestContinuation (IClient client, Request request)
        {
            Client = client;
            Request = request;
        }

        RequestContinuation (IClient client, Request request, IContinuation continuation)
        {
            Client = client;
            Request = request;
            this.continuation = continuation;
        }

        public override Response.Builder Run ()
        {
            try {
                if (continuation == null)
                    return Services.Instance.HandleRequest (Request);
                else
                    return Services.Instance.HandleRequest (Request, continuation);
            } catch (YieldException e) {
                throw new YieldException (new RequestContinuation (Client, Request, e.Continuation));
            }
        }
    };
}
