using System;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    /// <summary>
    /// Main KRPC service, used by clients to interact with basic server functionality.
    /// This includes requesting a description of the available services.
    /// </summary>
    [KRPCService]
    public static class KRPC
    {
        /// <summary>
        /// Returns some information about the server, such as the version.
        /// </summary>
        [KRPCProcedure]
        public static Schema.KRPC.Status GetStatus ()
        {
            var status = Schema.KRPC.Status.CreateBuilder ();
            status.Version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version.ToString ();
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
                    var procedure = Schema.KRPC.Procedure.CreateBuilder ();
                    procedure.Name = procedureSignature.Name;
                    if (procedureSignature.HasReturnType)
                        procedure.ReturnType = TypeUtils.GetTypeName (procedureSignature.ReturnType);
                    foreach (var parameterSignature in procedureSignature.Parameters) {
                        var parameter = Schema.KRPC.Parameter.CreateBuilder ();
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
                services.AddServices_ (service);
            }
            Schema.KRPC.Services result = services.Build ();
            return result;
        }
    }
}
