using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Scanner that finds service signatures from all loaded assemblies.
    /// </summary>
    public static class Scanner
    {
        /// <summary>
        /// The current assembly being scanned, when GetServices being run.
        /// </summary>
        public static Assembly CurrentAssembly { get; private set; }

        public static bool CheckDocumented { get; set; }

        /// <summary>
        /// Find all service signatures from all loaded assemblies.
        /// Errors are added to the given error list.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public static IDictionary<string, ServiceSignature> GetServices (IList<string> errors = null)
        {
            var serviceIds = new HashSet<uint> ();
            IDictionary<string, ServiceSignature> signatures = new Dictionary<string, ServiceSignature> ();

            // Scan for static classes annotated with KRPCService
            var serviceTypes = Reflection.GetTypesWith<KRPCServiceAttribute> ().ToList ();
            foreach (var serviceType in serviceTypes) {
                try {
                    CurrentAssembly = serviceType.Assembly;
                    var serviceId = TypeUtils.GetServiceId (serviceType);
                    if (serviceIds.Contains (serviceId))
                        HandleError(errors, "service " + TypeUtils.GetServiceName(serviceType), "Service id clashes with another service");
                    serviceIds.Add (serviceId);
                    var service = new ServiceSignature (serviceType, serviceId);
                    if (signatures.ContainsKey (service.Name))
                        service = signatures [service.Name];
                    else
                        signatures [service.Name] = service;
                    // Add procedures
                    foreach (var method in Reflection.GetMethodsWith<KRPCProcedureAttribute> (serviceType)) {
                        try {
                            service.AddProcedure (method);
                        } catch (ServiceException exn) {
                            HandleError(errors, "service " + service.Name, exn);
                        }
                    }
                    // Add properties
                    foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (serviceType)) {
                        try {
                            service.AddProperty (property);
                        } catch (ServiceException exn) {
                            HandleError(errors, "service " + service.Name, exn);
                        }
                    }
                    // Check for methods
                    var invalidMethod = Reflection.GetMethodsWith<KRPCMethodAttribute> (serviceType).FirstOrDefault ();
                    if (invalidMethod != null)
                        HandleError(errors, "service " + service.Name, "Service contains a class method " + invalidMethod.Name);
                } catch (ServiceException exn) {
                    HandleError(errors, string.Empty, exn);
                }
            }

            // Scan for classes annotated with KRPCClass
            foreach (var classType in Reflection.GetTypesWith<KRPCClassAttribute> ()) {
                try {
                    CurrentAssembly = classType.Assembly;
                    TypeUtils.ValidateKRPCClass (classType);
                    var serviceName = TypeUtils.GetClassServiceName (classType);
                    if (!signatures.ContainsKey (serviceName))
                        HandleError(errors, "service " + serviceName, "Service does not exist, when loading class");
                    var service = signatures [serviceName];
                    var cls = service.AddClass (classType);
                    // Add class methods
                    foreach (var method in Reflection.GetMethodsWith<KRPCMethodAttribute> (classType)) {
                        try {
                            service.AddClassMethod (cls, classType, method);
                        } catch (ServiceException exn) {
                            HandleError(errors, "service " + serviceName + ", class " + cls, exn);
                        }
                    }
                    // Add class properties
                    foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (classType)) {
                        try {
                            service.AddClassProperty (cls, classType, property);
                        } catch (ServiceException exn) {
                            HandleError(errors, "service " + serviceName + ", class " + cls, exn);
                        }
                    }
                } catch (ServiceException exn) {
                    HandleError(errors, string.Empty, exn);
                }
            }

            // Scan for enumerations annotated with KRPCEnum
            foreach (var enumType in Reflection.GetTypesWith<KRPCEnumAttribute> ()) {
                try {
                    CurrentAssembly = enumType.Assembly;
                    TypeUtils.ValidateKRPCEnum (enumType);
                    var serviceName = TypeUtils.GetEnumServiceName (enumType);
                    if (!signatures.ContainsKey (serviceName))
                        HandleError(errors, "service " + serviceName, "Service does not exist, when loading enumeration");
                    var service = signatures [serviceName];
                    service.AddEnum (enumType);
                } catch (ServiceException exn) {
                    HandleError(errors, string.Empty, exn);
                }
            }

            // Scan for classes annotated with KRPCException
            foreach (var exnType in Reflection.GetTypesWith<KRPCExceptionAttribute> ()) {
                try {
                    CurrentAssembly = exnType.Assembly;
                    TypeUtils.ValidateKRPCException (exnType);
                    var serviceName = TypeUtils.GetExceptionServiceName (exnType);
                    if (!signatures.ContainsKey (serviceName))
                        HandleError(errors, "service " + serviceName, "Service does not exist, when loading exception");
                    var service = signatures [serviceName];
                    service.AddException (exnType);
                } catch (ServiceException exn) {
                    HandleError(errors, string.Empty, exn);
                }
            }

            CurrentAssembly = null;

            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                HandleError(errors, string.Empty, "KRPC service could not be found");

            return signatures;
        }

        static void HandleError(IList<string> errors, string context, string msg) {
            if (context.Length > 0)
                msg = "In " + context + ": " + msg;
            HandleError(errors, new ServiceException(msg));
        }

        static void HandleError(IList<string> errors, Exception exn) {
            if (errors != null)
                errors.Add(exn.Message);
            else
                throw exn;
        }

        static void HandleError(IList<string> errors, string context, Exception exn) {
            if (errors != null) {
                var msg = exn.Message;
                if (context.Length > 0)
                    msg = "In " + context + ": " + msg;
                errors.Add(msg);
            } else {
                throw exn;
            }
        }

        /// <summary>
        /// Get mapping from exception types to kRPC exception types.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        public static IDictionary<Type, Type> GetMappedExceptionTypes()
        {
            IDictionary<Type, Type> mappedExceptionTypes = new Dictionary<Type, Type> ();
            foreach (var exnType in Reflection.GetTypesWith<KRPCExceptionAttribute> ()) {
                TypeUtils.ValidateKRPCException (exnType);
                var mappedExnType = Reflection.GetAttribute<KRPCExceptionAttribute> (exnType).MappedException;
                if (mappedExnType != null && !mappedExceptionTypes.ContainsKey (mappedExnType))
                    mappedExceptionTypes [mappedExnType] = exnType;
            }
            return mappedExceptionTypes;
        }
    }
}
