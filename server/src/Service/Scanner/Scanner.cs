using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    static class Scanner
    {
        public static Assembly CurrentAssembly { get; private set; }

        public static bool CheckDocumented { get; set; }

        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static IDictionary<string, ServiceSignature> GetServices ()
        {
            IDictionary<string, ServiceSignature> signatures = new Dictionary<string, ServiceSignature> ();

            // Scan for static classes annotated with KRPCService

            // FIXME: Following is a hack to workaround a bug in Reflection.GetTypesWith
            // When running unit tests, Service.KRPC is not found as it contains types that depend on UnityEngine
            var serviceTypes = Reflection.GetTypesWith<KRPCServiceAttribute> ().ToList ();
            if (!serviceTypes.Contains (typeof(KRPC)))
                serviceTypes.Add (typeof(KRPC));

            foreach (var serviceType in serviceTypes) {
                CurrentAssembly = serviceType.Assembly;
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
                // Check for methods
                var invalidMethod = Reflection.GetMethodsWith<KRPCMethodAttribute> (serviceType).FirstOrDefault ();
                if (invalidMethod != null)
                    throw new ServiceException ("Service " + service.Name + " contains a class method " + invalidMethod.Name);
            }

            // Scan for classes annotated with KRPCClass
            foreach (var classType in Reflection.GetTypesWith<KRPCClassAttribute> ()) {
                CurrentAssembly = classType.Assembly;
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

            // Scan for enumerations annotated with KRPCEnum
            foreach (var enumType in Reflection.GetTypesWith<KRPCEnumAttribute> ()) {
                CurrentAssembly = enumType.Assembly;
                TypeUtils.ValidateKRPCEnum (enumType);
                var serviceName = TypeUtils.GetEnumServiceName (enumType);
                if (!signatures.ContainsKey (serviceName))
                    signatures [serviceName] = new ServiceSignature (serviceName);
                var service = signatures [serviceName];
                service.AddEnum (enumType);
            }

            CurrentAssembly = null;

            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                throw new ServiceException ("KRPC service could not be found");

            return signatures;
        }
    }
}
