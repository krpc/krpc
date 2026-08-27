using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Find all service signatures from all loaded assemblies.
        /// Errors are added to the given error list.
        /// </summary>
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
                    // Check for class members declared in the service
                    var invalidMethod = Reflection.GetMethodsWith<KRPCMethodAttribute> (serviceType).FirstOrDefault ();
                    if (invalidMethod != null)
                        HandleError(errors, "service " + service.Name, "Service contains a class method " + invalidMethod.Name);
                    CheckForPropertyMethod (errors, "service " + service.Name, serviceType);
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
                    CheckForPropertyMethod (errors, "service " + serviceName + ", class " + cls, classType);
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

            // Scan for structures annotated with KRPCStruct. Every one found is validated,
            // including its fields, whether or not any procedure refers to it
            foreach (var structType in Reflection.GetTypesWith<KRPCStructAttribute> ()) {
                try {
                    CurrentAssembly = structType.Assembly;
                    TypeUtils.ValidateKRPCStruct (structType);
                    var serviceName = TypeUtils.GetStructServiceName (structType);
                    if (!signatures.ContainsKey (serviceName))
                        HandleError(errors, "service " + serviceName, "Service does not exist, when loading struct");
                    var service = signatures [serviceName];
                    service.AddStruct (structType);
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

            // Extension members are methods in public static classes that add a member to
            // another service's class. They are scanned last, once every class is registered
            var unreachable = new HashSet<Type> ();
            foreach (var method in Reflection.GetStaticClassMethodsWith<KRPCMethodAttribute> ())
                AddExtensionMember (signatures, errors, unreachable, method, false);
            foreach (var method in Reflection.GetStaticClassMethodsWith<KRPCPropertyAttribute> ())
                AddExtensionMember (signatures, errors, unreachable, method, true);

            CurrentAssembly = null;

            // Check that the main KRPC service was found
            if (!signatures.ContainsKey ("KRPC"))
                HandleError(errors, string.Empty, "KRPC service could not be found");

            return signatures;
        }

        /// <summary>
        /// Report a KRPCProperty applied to a method of the given type. Only an extension method
        /// declares a property that way, and the extension pass skips services and classes.
        /// </summary>
        static void CheckForPropertyMethod (IList<string> errors, string context, Type type)
        {
            var method = Reflection.GetMethodsWith<KRPCPropertyAttribute> (type).FirstOrDefault ();
            if (method != null)
                HandleError (errors, context, "KRPCProperty is applied to the method " + method.Name +
                             "; a method declares a property only as an extension member, in a public static class outside a service");
        }

        static void AddExtensionMember (IDictionary<string, ServiceSignature> signatures, IList<string> errors,
                                        ISet<Type> unreachable, MethodInfo method, bool isProperty)
        {
            var declaringType = method.DeclaringType;
            // Members declared in a service are handled when scanning the service itself
            if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                return;
            // A class that is not public is out of reach of the assembly the server runs from,
            // so its members are named in a warning
            if (!(declaringType.IsPublic || declaringType.IsNestedPublic)) {
                if (unreachable.Add (declaringType))
                    Logger.WriteLine (
                        "Ignoring the extension members of " + declaringType + ", as the class is not public",
                        Logger.Severity.Warning);
                return;
            }
            try {
                CurrentAssembly = declaringType.Assembly;
                var classType = TypeUtils.GetExtensionTargetClass (method);
                var serviceName = TypeUtils.GetClassServiceName (classType);
                if (!signatures.ContainsKey (serviceName)) {
                    HandleError (errors, "service " + serviceName, "Service does not exist, when loading extension member");
                    return;
                }
                var service = signatures [serviceName];
                var cls = classType.Name;
                try {
                    if (isProperty)
                        service.AddClassExtensionProperty (cls, classType, method);
                    else
                        service.AddClassExtensionMethod (cls, classType, method);
                } catch (ServiceException exn) {
                    HandleError (errors, "service " + serviceName + ", class " + cls, exn);
                }
            } catch (ServiceException exn) {
                HandleError (errors, string.Empty, exn);
            }
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
