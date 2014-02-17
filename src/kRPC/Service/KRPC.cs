using System;
using Google.ProtocolBuffers;
using KRPC.Utils;

namespace KRPC.Service
{
    [KRPCService]
    public class KRPC
    {
        [KRPCMethod]
        public static Schema.KRPC.Services GetServices ()
        {
            var services = Schema.KRPC.Services.CreateBuilder ();

            foreach (var serviceType in Reflection.GetTypesWith<KRPCService> ()) {
                var service = Schema.KRPC.Service.CreateBuilder ();
                service.SetName (serviceType.Name);

                foreach (var methodType in Reflection.GetMethodsWith<KRPCMethod> (serviceType)) {
                    var method = Schema.KRPC.Method.CreateBuilder ();
                    method.Name = methodType.Name;
                    if (methodType.ReturnType != typeof(void))
                        method.ReturnType = Reflection.GetMessageType (methodType.ReturnType);
                    if (methodType.GetParameters ().Length == 1)
                        method.ParameterType = Reflection.GetMessageType (methodType.GetParameters () [0].ParameterType);
                    //TODO: check if there is more than one parameter - it's not allowed
                    service.AddMethods (method);
                }

                services.AddServices_ (service);
            }

            Schema.KRPC.Services result = services.Build ();
            Logger.WriteLine (result.ToString());
            return result;
        }
    }
}
