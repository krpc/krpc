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
                    if (!CheckServiceNotDeclared (signatures, serviceType, errors))
                        continue;
                    var serviceId = TypeUtils.GetServiceId (serviceType);
                    if (serviceId == 0)
                        HandleError(errors, "service " + TypeUtils.GetServiceName(serviceType), "Service has id 0");
                    if (serviceIds.Contains (serviceId))
                        HandleError(errors, "service " + TypeUtils.GetServiceName(serviceType), "Service id clashes with another service");
                    serviceIds.Add (serviceId);
                    var service = new ServiceSignature (serviceType, serviceId);
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

            CheckServiceNames (signatures.Values, errors);
            foreach (var service in signatures.Values) {
                CheckProcedureIds (service, errors);
                CheckTypes (service, errors);
                CheckMemberNames (service, errors);
            }
            return signatures;
        }

        /// <summary>
        /// Check that no two procedures in the service share an id, and that none has an id of 0.
        /// </summary>
        internal static void CheckProcedureIds (ServiceSignature service, IList<string> errors)
        {
            foreach (var group in service.Procedures.Values.GroupBy (x => x.Id).OrderBy (x => x.Key)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (group.Key == 0) {
                    foreach (var name in names)
                        HandleError (errors, "service " + service.Name, "Procedure " + name + " has id 0");
                } else if (names.Count > 1) {
                    HandleError (errors, "service " + service.Name,
                        "Procedures " + string.Join (", ", names.ToArray ()) + " share the id " + group.Key);
                }
            }
        }

        /// <summary>
        /// Check that no other class declares the service the given class declares.
        /// </summary>
        internal static bool CheckServiceNotDeclared (IDictionary<string, ServiceSignature> signatures, Type serviceType, IList<string> errors)
        {
            var name = TypeUtils.GetServiceName (serviceType);
            ServiceSignature existing;
            if (!signatures.TryGetValue (name, out existing))
                return true;
            HandleError (errors, "service " + name,
                "Service is declared by both " + existing.UnderlyingType + " and " + serviceType);
            return false;
        }

        /// <summary>
        /// Check that the services have unique names, compared case-insensitively.
        /// </summary>
        internal static void CheckServiceNames (IEnumerable<ServiceSignature> services, IList<string> errors)
        {
            foreach (var group in services.GroupBy (x => x.Name.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var names = group.Select (x => x.Name).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (names.Count > 1)
                    HandleError (errors, string.Empty, "Services " + string.Join (", ", names.ToArray ()) + " share a name, compared case-insensitively");
            }
        }

        /// <summary>
        /// Check that no two members of a scope share a name, compared case-insensitively. The
        /// scopes are the service, each class, enumeration and structure, and the parameters of
        /// each procedure. A property also takes the names of its accessors, so a property Foo
        /// shares a name with a method GetFoo.
        /// </summary>
        internal static void CheckMemberNames (ServiceSignature service, IList<string> errors)
        {
            var context = "service " + service.Name;
            // The names each member takes, by scope and then by member
            var scopes = new Dictionary<string, Dictionary<string, string[]>> ();
            foreach (var procedure in service.Procedures.Keys.OrderBy (x => x, StringComparer.Ordinal)) {
                // A procedure name is a member name, after its class name and get, set or static
                var parts = procedure.Split ('_').ToList ();
                var scope = service.Name;
                if (parts.Count > 1 && parts [0] != "get" && parts [0] != "set") {
                    scope += "." + parts [0];
                    parts.RemoveAt (0);
                }
                var member = parts [parts.Count - 1];
                var property = parts [0] == "get" || parts [0] == "set";
                var names = property ? new [] { member, "Get" + member, "Set" + member } : new [] { member };
                Dictionary<string, string[]> members;
                if (!scopes.TryGetValue (scope, out members))
                    scopes [scope] = members = new Dictionary<string, string[]> ();
                // The getter and setter of a property are one member
                var kind = property ? " (property)" : parts [0] == "static" ? " (static)" : string.Empty;
                members [scope + "." + member + kind] = names;
            }
            foreach (var members in scopes.OrderBy (x => x.Key, StringComparer.Ordinal).Select (x => x.Value))
                CheckNames (members.SelectMany (x => x.Value.Select (name => new KeyValuePair<string, string> (name, x.Key))), "Members", context, errors);
            foreach (var enumeration in service.Enumerations.Values.OrderBy (x => x.Name, StringComparer.Ordinal))
                CheckNames (enumeration.Values.Select (x => new KeyValuePair<string, string> (x.Name, x.FullyQualifiedName)), "Values", context, errors);
            foreach (var structure in service.Structs.Values.OrderBy (x => x.Name, StringComparer.Ordinal))
                CheckNames (structure.Fields.Select (x => new KeyValuePair<string, string> (x.Name, x.FullyQualifiedName)), "Fields", context, errors);
            foreach (var procedure in service.Procedures.Values.OrderBy (x => x.Name, StringComparer.Ordinal))
                CheckNames (procedure.Parameters.Select (x => new KeyValuePair<string, string> (x.Name, procedure.FullyQualifiedName + "(" + x.Name + ")")), "Parameters", context, errors);
        }

        /// <summary>
        /// Report each set of definitions that share a name, compared case-insensitively, given
        /// as pairs of a name and the definition that takes it. A set is reported once.
        /// </summary>
        static void CheckNames (IEnumerable<KeyValuePair<string, string>> names, string kind, string context, IList<string> errors)
        {
            var reported = new HashSet<string> ();
            foreach (var group in names.GroupBy (x => x.Key.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var definitions = group.Select (x => x.Value).Distinct ().OrderBy (x => x, StringComparer.Ordinal).ToArray ();
                var list = string.Join (", ", definitions);
                if (definitions.Length > 1 && reported.Add (list))
                    HandleError (errors, context, kind + " " + list + " share the name " + group.Select (x => x.Key).OrderBy (x => x, StringComparer.Ordinal).First () + ", compared case-insensitively");
            }
        }

        /// <summary>
        /// Check that the classes, enumerations, structures and exceptions of the service have
        /// unique names, compared case-insensitively, none of them the name of the service, and
        /// unique ids, none of them 0.
        /// </summary>
        internal static void CheckTypes (ServiceSignature service, IList<string> errors)
        {
            var types = service.Classes.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id })
                .Concat (service.Enumerations.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id }))
                .Concat (service.Structs.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id }))
                .Concat (service.Exceptions.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id }))
                .ToList ();
            var context = "service " + service.Name;
            foreach (var type in types.Where (x => string.Equals (x.Name, service.Name, StringComparison.OrdinalIgnoreCase)).OrderBy (x => x.Name, StringComparer.Ordinal))
                HandleError (errors, context, "Type " + type.FullyQualifiedName + " has the name of its service, compared case-insensitively");
            foreach (var group in types.GroupBy (x => x.Name.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (names.Count > 1)
                    HandleError (errors, context, "Types " + string.Join (", ", names.ToArray ()) + " share a name, compared case-insensitively");
            }
            foreach (var group in types.GroupBy (x => x.Id).OrderBy (x => x.Key)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (group.Key == 0) {
                    foreach (var name in names)
                        HandleError (errors, context, "Type " + name + " has id 0");
                } else if (names.Count > 1 && group.Select (x => x.Name).Distinct ().Count () > 1) {
                    // Types of one name share a hashed id, and are reported as sharing the name
                    HandleError (errors, context, "Types " + string.Join (", ", names.ToArray ()) + " share the id " + group.Key);
                }
            }
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
