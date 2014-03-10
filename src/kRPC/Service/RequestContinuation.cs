namespace KRPC.
{
    /// <summary>
    /// A continuation that runs a request object. Captures the common case where a
    /// request always returns a result, and never throws YieldException
    /// </summary>
    class RequestContinuation : Continuation<Response.Builder>
    {
        Request request;

        RequestContinuation (Request request)
        {
            self.request = request;
        }

        Response.Builder Run ()
        {
            return KRPC.Service.Services.Instance.HandleRequest (request);
        }
    };
}
