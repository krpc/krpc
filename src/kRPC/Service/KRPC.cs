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
