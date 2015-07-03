using System;
using KRPC.Schema.KRPC;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// Main KRPC service, used by clients to interact with basic server functionality.
    /// This includes requesting a description of the available services and setting up streams.
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
            status.BytesRead = KRPCServer.Context.Server.BytesRead;
            status.BytesWritten = KRPCServer.Context.Server.BytesWritten;
            status.BytesReadRate = KRPCServer.Context.Server.BytesReadRate;
            status.BytesWrittenRate = KRPCServer.Context.Server.BytesWrittenRate;
            status.RpcsExecuted = KRPCServer.Context.Server.RPCsExecuted;
            status.RpcRate = KRPCServer.Context.Server.RPCRate;
            status.OneRpcPerUpdate = KRPCServer.Context.Server.OneRPCPerUpdate;
            status.MaxTimePerUpdate = KRPCServer.Context.Server.MaxTimePerUpdate;
            status.AdaptiveRateControl = KRPCServer.Context.Server.AdaptiveRateControl;
            status.BlockingRecv = KRPCServer.Context.Server.BlockingRecv;
            status.RecvTimeout = KRPCServer.Context.Server.RecvTimeout;
            status.TimePerRpcUpdate = KRPCServer.Context.Server.TimePerRPCUpdate;
            status.PollTimePerRpcUpdate = KRPCServer.Context.Server.PollTimePerRPCUpdate;
            status.ExecTimePerRpcUpdate = KRPCServer.Context.Server.ExecTimePerRPCUpdate;
            status.StreamRpcs = KRPCServer.Context.Server.StreamRPCs;
            status.StreamRpcsExecuted = KRPCServer.Context.Server.StreamRPCsExecuted;
            status.StreamRpcRate = KRPCServer.Context.Server.StreamRPCRate;
            status.TimePerStreamUpdate = KRPCServer.Context.Server.TimePerStreamUpdate;
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
                    service.AddProcedures (procedure);
                }
                foreach (var clsName in serviceSignature.Classes) {
                    var cls = Class.CreateBuilder ();
                    cls.Name = clsName;
                    service.AddClasses (cls);
                }
                foreach (var enumName in serviceSignature.Enums.Keys) {
                    var enm = Enumeration.CreateBuilder ();
                    enm.Name = enumName;
                    foreach (var enumValueName in serviceSignature.Enums[enumName].Keys) {
                        var enmValue = EnumerationValue.CreateBuilder ();
                        enmValue.Name = enumValueName;
                        enmValue.Value = serviceSignature.Enums [enumName] [enumValueName];
                        enm.AddValues (enmValue);
                    }
                    service.AddEnumerations (enm);
                }
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
