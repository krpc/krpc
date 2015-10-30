using System;
using KRPC.Schema.KRPC;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// Main kRPC service, used by clients to interact with basic server functionality.
    /// </summary>
    [KRPCService]
    public static class KRPC
    {
        /// <summary>
        /// Returns some information about the server, such as the version.
        /// </summary>
        [KRPCProcedure]
        public static Status GetStatus ()
        {
            var status = Status.CreateBuilder ();
            var version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version;
            status.Version = version.Major + "." + version.Minor + "." + version.Build;
            var server = KRPCServer.Context.Server;
            status.BytesRead = server != null ? server.BytesRead : 0;
            status.BytesWritten = server != null ? server.BytesWritten : 0;
            status.BytesReadRate = server != null ? server.BytesReadRate : 0;
            status.BytesWrittenRate = server != null ? server.BytesWrittenRate : 0;
            status.RpcsExecuted = server != null ? server.RPCsExecuted : 0;
            status.RpcRate = server != null ? server.RPCRate : 0;
            status.OneRpcPerUpdate = server != null ? server.OneRPCPerUpdate : false;
            status.MaxTimePerUpdate = server != null ? server.MaxTimePerUpdate : 0;
            status.AdaptiveRateControl = server != null ? server.AdaptiveRateControl : false;
            status.BlockingRecv = server != null ? server.BlockingRecv : false;
            status.RecvTimeout = server != null ? server.RecvTimeout : 0;
            status.TimePerRpcUpdate = server != null ? server.TimePerRPCUpdate : 0;
            status.PollTimePerRpcUpdate = server != null ? server.PollTimePerRPCUpdate : 0;
            status.ExecTimePerRpcUpdate = server != null ? server.ExecTimePerRPCUpdate : 0;
            status.StreamRpcs = server != null ? server.StreamRPCs : 0;
            status.StreamRpcsExecuted = server != null ? server.StreamRPCsExecuted : 0;
            status.StreamRpcRate = server != null ? server.StreamRPCRate : 0;
            status.TimePerStreamUpdate = server != null ? server.TimePerStreamUpdate : 0;
            return status.Build ();
        }

        /// <summary>
        /// Returns information on all services, procedures, classes, properties etc. provided by the server.
        /// Can be used by client libraries to automatically create functionality such as stubs.
        /// </summary>
        [KRPCProcedure]
        public static Schema.KRPC.Services GetServices ()
        {
            var services = Schema.KRPC.Services.CreateBuilder ();
            foreach (var serviceSignature in Services.Instance.Signatures.Values) {
                var service = Schema.KRPC.Service.CreateBuilder ();
                service.SetName (serviceSignature.Name);
                foreach (var procedureSignature in serviceSignature.Procedures.Values) {
                    var procedure = Procedure.CreateBuilder ();
                    procedure.Name = procedureSignature.Name;
                    if (procedureSignature.HasReturnType)
                        procedure.ReturnType = TypeUtils.GetTypeName (procedureSignature.ReturnType);
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = Parameter.CreateBuilder ();
                        parameter.Name = parameterSignature.Name;
                        parameter.Type = TypeUtils.GetTypeName (parameterSignature.Type);
                        if (parameterSignature.HasDefaultArgument)
                            parameter.DefaultArgument = parameterSignature.DefaultArgument;
                        procedure.AddParameters (parameter);
                    }
                    foreach (var attribute in procedureSignature.Attributes) {
                        procedure.AddAttributes (attribute);
                    }
                    if (procedureSignature.Documentation != "")
                        procedure.SetDocumentation (procedureSignature.Documentation);
                    service.AddProcedures (procedure);
                }
                foreach (var clsSignature in serviceSignature.Classes.Values) {
                    var cls = Class.CreateBuilder ();
                    cls.Name = clsSignature.Name;
                    if (clsSignature.Documentation != "")
                        cls.Documentation = clsSignature.Documentation;
                    service.AddClasses (cls);
                }
                foreach (var enmSignature in serviceSignature.Enums.Values) {
                    var enm = Enumeration.CreateBuilder ();
                    enm.Name = enmSignature.Name;
                    if (enmSignature.Documentation != "")
                        enm.Documentation = enmSignature.Documentation;
                    foreach (var enmValueSignature in enmSignature.Values) {
                        var enmValue = EnumerationValue.CreateBuilder ();
                        enmValue.Name = enmValueSignature.Key;
                        enmValue.Value = enmValueSignature.Value.Value;
                        if (enmValueSignature.Value.Documentation != "")
                            enmValue.Documentation = enmValueSignature.Value.Documentation;
                        enm.AddValues (enmValue);
                    }
                    service.AddEnumerations (enm);
                }
                if (serviceSignature.Documentation != "")
                    service.SetDocumentation (serviceSignature.Documentation);
                services.AddServices_ (service);
            }
            Schema.KRPC.Services result = services.Build ();
            return result;
        }

        /// <summary>
        /// Add a streaming request and return its identifier.
        /// </summary>
        [KRPCProcedure]
        public static uint AddStream (Request request)
        {
            return KRPCServer.Context.Server.AddStream (KRPCServer.Context.RPCClient, request);
        }

        /// <summary>
        /// Remove a streaming request.
        /// </summary>
        [KRPCProcedure]
        public static void RemoveStream (uint id)
        {
            KRPCServer.Context.Server.RemoveStream (KRPCServer.Context.RPCClient, id);
        }
    }
}
