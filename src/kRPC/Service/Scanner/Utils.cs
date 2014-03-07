using System;
using KRPC.Utils;
using KRPC.Service.Attributes;

namespace KRPC.Service.Scanner
{
    static class Utils
    {
        /// <summary>
        /// Returns the service that the given KRPCClass type is defined in.
        /// </summary>
        public static string GetServiceFor (Type classType)
        {
            foreach (var service in Reflection.GetTypesWith<KRPCService> ()) {
                foreach (var cls in Reflection.GetClassesWith<KRPCClass> (service)) {
                    if (cls == classType)
                        return service.Name;
                }
            }
            throw new ArgumentException ();
        }
    }
}

