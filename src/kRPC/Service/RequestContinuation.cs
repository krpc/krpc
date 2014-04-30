using KRPC.Schema.KRPC;
using KRPC.Continuations;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a request object. Captures the common case where a
    /// request always returns a result, and never throws YieldException
    /// </summary>
    public class RequestContinuation : Continuation<Response.Builder>
    {
        public Request request;
        IContinuation continuation;

        public RequestContinuation (Request request)
        {
            this.request = request;
        }

        public RequestContinuation (Request request, IContinuation continuation)
        {
            this.request = request;
            this.continuation = continuation;
        }

        public override Response.Builder Run ()
        {
            try {
                if (continuation == null)
                    return Services.Instance.HandleRequest (request);
                else
                    return Services.Instance.HandleRequest (request, continuation);
            } catch (YieldException e) {
                throw new YieldException (new RequestContinuation (request, e.Continuation));
            }
        }
    };
}
