using System;
using KRPC.Utils;

namespace KRPC.Service
{
    [KRPCService]
    public static class KRPC
    {
        [KRPCProcedure]
        public static Schema.KRPC.Status GetStatus ()
        {
            var status = Schema.KRPC.Status.CreateBuilder ();
            status.Version = System.Reflection.Assembly.GetExecutingAssembly ().GetName ().Version.ToString ();
            return status.Build ();
        }

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
                        procedure.ReturnType = ProtocolBuffers.GetTypeName (procedureSignature.ReturnType);
                    foreach (var parameterType in procedureSignature.ParameterTypes) {
                        procedure.AddParameterTypes (ProtocolBuffers.GetTypeName (parameterType));
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
