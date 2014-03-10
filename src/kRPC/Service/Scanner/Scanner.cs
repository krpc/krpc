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
            ValidateAttributes ();

            // Find all services
            var serviceTypes = Reflection.GetTypesWith<KRPCServiceAttribute> ();

            IDictionary<string, ServiceSignature> signatures;
            try {
                signatures = serviceTypes
                    .Select (x => new ServiceSignature (x))
                    .ToDictionary (x => x.Name);
            } catch (ArgumentException) {
                // Handle service name clashes
                var duplicates = serviceTypes
                    .Select (x => x.Name)
                    .Duplicates ()
                    .ToArray ();
                throw new ServiceException (
                    "Multiple Services have the same name. " +
                    "Duplicates are " + String.Join (", ", duplicates));
            }
            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                throw new ServiceException ("KRPC service could not be found");
            // TODO: Find KRPCClassAttribute annotated classes at top-level with the Service field set
            return signatures;
        }

        public static void ValidateAttributes ()
        {
            foreach (var type in Reflection.GetTypesWith<KRPCServiceAttribute> ()) {
                TypeUtils.ValidateKRPCService (type);
                foreach (var method in Reflection.GetMethodsWith<KRPCProcedureAttribute> (type))
                    TypeUtils.ValidateKRPCProcedure (method);
                foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (type))
                    TypeUtils.ValidateKRPCProperty (property);
            }
            foreach (var cls in Reflection.GetTypesWith<KRPCClassAttribute> ()) {
                TypeUtils.ValidateKRPCClass (cls);
                foreach (var method in Reflection.GetMethodsWith<KRPCMethodAttribute> (cls))
                    TypeUtils.ValidateKRPCMethod (method);
                foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (cls))
                    TypeUtils.ValidateKRPCProperty (property);
            }
        }
    }
}

