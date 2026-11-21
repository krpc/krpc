using System;
using System.Text;
using System.Threading.Tasks;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.KSA.Grpc
{
    /// <summary>
    /// Registers a unary gRPC method for each procedure of the services, named
    /// <c>krpc.(service in lower case).(service)/(procedure)</c>. The methods take and return
    /// the message bytes, which <see cref="ProtoCodec"/> encodes, so no generated code is
    /// needed on the server.
    /// </summary>
    sealed class ProcedureMethods : IServiceMethodProvider<ProcedureMethods.Service>
    {
        /// <summary>
        /// The service type the methods are registered against. It holds no state.
        /// </summary>
        public sealed class Service
        {
        }

        static readonly Marshaller<byte[]> bytes = Marshallers.Create (x => x, x => x);

        readonly GrpcServer server;

        public ProcedureMethods (GrpcServer server)
        {
            this.server = server;
        }

        /// <summary>
        /// The number of methods registered.
        /// </summary>
        public int Count { get; private set; }

        public void OnServiceMethodDiscovery (ServiceMethodProviderContext<Service> context)
        {
            Count = 0;
            foreach (var service in KRPC.Service.Services.Instance.Signatures.Values) {
                // The KRPC service's streams, events and expressions have no unary form
                if (service.Name == "KRPC")
                    continue;
                var package = "krpc." + service.Name.ToLowerInvariant () + ".";
                foreach (var procedure in service.Procedures.Values) {
                    if (!ProtoCodec.IsSupported (procedure, out var reason)) {
                        Logger.WriteLine (
                            "gRPC server: skipping " + procedure.FullyQualifiedName + ", " + reason,
                            Logger.Severity.Warning);
                        continue;
                    }
                    var (holder, name) = GrpcName (service.Name, procedure.Name);
                    var method = new Method<byte[], byte[]> (MethodType.Unary, package + holder + "Service", name, bytes, bytes);
                    var signature = procedure;
                    context.AddUnaryMethod (method, Array.Empty<object> (),
                        (instance, request, call) => Invoke (signature, request, call));
                    Count++;
                }
            }
        }

        /// <summary>
        /// The class or service whose gRPC service holds a procedure, and the procedure's method
        /// name, as protogen.py names them. C_get_Name is CService.GetName, C_static_Create is
        /// CService.Create and get_UT is the service's GetUT.
        /// </summary>
        internal static (string Holder, string Name) GrpcName (string service, string procedure)
        {
            var parts = procedure.Split ('_');
            var holder = service;
            var i = 0;
            if (parts.Length > 1 && parts [0] != "get" && parts [0] != "set") {
                holder = parts [0];
                i = 1;
            }
            switch (parts [i]) {
            case "get":
                return (holder, "Get" + parts [i + 1]);
            case "set":
                return (holder, "Set" + parts [i + 1]);
            case "static":
                return (holder, parts [i + 1]);
            default:
                return (holder, parts [i]);
            }
        }

        Task<byte[]> Invoke (ProcedureSignature procedure, byte[] request, ServerCallContext context)
        {
            var name = context.RequestHeaders.GetValue ("krpc-client-name") ?? string.Empty;
            var client = server.GetClient (context.GetHttpContext ().Connection.Id, name, context.Peer);
            var call = new PendingCall (procedure, request);
            client.Enqueue (call);
            // Note: the call runs to completion in game even when the client cancels it or its
            // deadline passes, and the response is dropped
            return call.Result;
        }

        /// <summary>
        /// Encode the response to a call, or the status for an error.
        /// </summary>
        public static byte[] EncodeResponse (ProcedureSignature procedure, Response response, out (StatusCode Code, string Message, Metadata Trailers)? status)
        {
            Error error = null;
            if (response.HasError)
                error = response.Error;
            else if (response.Results.Count != 1)
                error = new Error ("Expected 1 result, got " + response.Results.Count, string.Empty);
            else if (response.Results [0].HasError)
                error = response.Results [0].Error;
            if (error != null) {
                status = ErrorStatus (error);
                return null;
            }
            status = null;
            return ProtoCodec.EncodeResponse (procedure, response.Results [0].Value);
        }

        static (StatusCode, string, Metadata) ErrorStatus (Error error)
        {
            StatusCode code;
            switch (error.Name) {
            case "ArgumentException":
            case "ArgumentNullException":
            case "ArgumentOutOfRangeException":
                code = StatusCode.InvalidArgument;
                break;
            case "InvalidOperationException":
                code = StatusCode.FailedPrecondition;
                break;
            default:
                code = StatusCode.Unknown;
                break;
            }
            var trailers = new Metadata ();
            if (!string.IsNullOrEmpty (error.Service))
                trailers.Add ("krpc-error-service", error.Service);
            if (!string.IsNullOrEmpty (error.Name))
                trailers.Add ("krpc-error-name", error.Name);
            if (!string.IsNullOrEmpty (error.StackTrace))
                trailers.Add ("krpc-error-stack-trace-bin", Encoding.UTF8.GetBytes (error.StackTrace));
            return (code, error.Description, trailers);
        }
    }
}
