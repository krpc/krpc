using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Continuations;
using KRPC.Server;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;

namespace KRPC.Service
{
    /// <summary>
    /// A continuation that runs a client request.
    /// </summary>
    sealed class RequestContinuation : Continuation<Response>
    {
        public IClient<Request,Response> Client { get; private set; }

        readonly Request request;
        readonly ProcedureSignature procedure;
        readonly Exception exception;
        readonly IContinuation continuation;

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public RequestContinuation (IClient<Request,Response> client, Request clientRequest)
        {
            Client = client;
            request = clientRequest;
            try {
                procedure = Services.Instance.GetProcedureSignature (request.Service, request.Procedure);
            } catch (Exception e) {
                exception = e;
            }
        }

        RequestContinuation (IClient<Request,Response> client, Request clientRequest, ProcedureSignature invokedProcedure, IContinuation currentContinuation)
        {
            Client = client;
            request = clientRequest;
            procedure = invokedProcedure;
            continuation = currentContinuation;
        }

        public override Response Run ()
        {
            if (exception != null)
                throw exception;
            try {
                var services = Services.Instance;
                if (continuation == null)
                    return services.HandleRequest (procedure, request);
                else
                    return services.HandleRequest (procedure, continuation);
            } catch (YieldException e) {
                throw new YieldException (new RequestContinuation (Client, request, procedure, e.Continuation));
            }
        }
    }
}
