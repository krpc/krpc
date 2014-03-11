using System;
using System.Linq;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    static class Scanner
    {
        public static IDictionary<string, ServiceSignature> GetServices ()
        {
            IDictionary<string, ServiceSignature> signatures = new Dictionary<string, ServiceSignature> ();

            // Scan for static classes annotated with KRPCService
            foreach (var serviceType in Reflection.GetTypesWith<KRPCServiceAttribute> ()) {
                var service = new ServiceSignature (serviceType);
                if (signatures.ContainsKey (service.Name))
                    service = signatures [service.Name];
                else
                    signatures [service.Name] = service;
                // Add procedures
                foreach (var method in Reflection.GetMethodsWith<KRPCProcedureAttribute> (serviceType))
                    service.AddProcedure (method);
                // Add properties
                foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (serviceType))
                    service.AddProperty (property);
            }

            // Scan for classes annotated with KRPCClass
            foreach (var classType in Reflection.GetTypesWith<KRPCClassAttribute> ()) {
                TypeUtils.ValidateKRPCClass (classType);
                var serviceName = TypeUtils.GetClassServiceName (classType);
                if (!signatures.ContainsKey (serviceName))
                    signatures [serviceName] = new ServiceSignature (serviceName);
                var service = signatures [serviceName];
                var cls = service.AddClass (classType);
                // Add class methods
                foreach (var method in Reflection.GetMethodsWith<KRPCMethodAttribute> (classType))
                    service.AddClassMethod (cls, method);
                // Add class properties
                foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (classType))
                    service.AddClassProperty (cls, property);
            }

            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                throw new ServiceException ("KRPC service could not be found");

            return signatures;
        }
    }
}

