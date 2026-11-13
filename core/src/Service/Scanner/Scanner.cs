using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        /// </summary>
        /// <remarks>
        /// With an error list, each error is added to it, and the member with the error is left
        /// out, along with every member that uses it. Without a warning list, warnings are
        /// errors. Without an error list, the first problem throws. Throws an exception if the
        /// KRPC service is not available.
        /// </remarks>
        public static IDictionary<string, ServiceSignature> GetServices (IList<string> errors = null, IList<string> warnings = null)
        {
            IDictionary<string, ServiceSignature> signatures = new Dictionary<string, ServiceSignature> ();
            var declaredTwice = new HashSet<string> ();

            // An assembly whose types fail to load is left out, as its services would be partial
            foreach (var failure in Reflection.GetUnloadableAssemblies (typeof(KRPCServiceAttribute).Assembly))
                HandleError (errors, string.Empty, "Assembly " + failure.Key.GetName ().Name + " could not be loaded, so its services are unavailable: " + failure.Value);

            // Scan for static classes annotated with KRPCService
            var serviceTypes = Reflection.GetTypesWith<KRPCServiceAttribute> ().ToList ();
            foreach (var serviceType in serviceTypes) {
                try {
                    CurrentAssembly = serviceType.Assembly;
                    if (!CheckServiceNotDeclared (signatures, serviceType, errors)) {
                        declaredTwice.Add (TypeUtils.GetServiceName (serviceType));
                        continue;
                    }
                    var service = new ServiceSignature (serviceType, TypeUtils.GetServiceId (serviceType));
                    signatures [service.Name] = service;
                    // Add procedures
                    foreach (var method in Reflection.GetMethodsWith<KRPCProcedureAttribute> (serviceType)) {
                        try {
                            service.AddProcedure (method);
                        } catch (Exception exn) when (IsScanError (exn)) {
                            HandleError(errors, "service " + service.Name, exn);
                        }
                    }
                    // Add properties
                    foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (serviceType)) {
                        try {
                            service.AddProperty (property);
                        } catch (Exception exn) when (IsScanError (exn)) {
                            HandleError(errors, "service " + service.Name, exn);
                        }
                    }
                    // Check for class members declared in the service
                    var invalidMethod = Reflection.GetMethodsWith<KRPCMethodAttribute> (serviceType).FirstOrDefault ();
                    if (invalidMethod != null)
                        HandleError(errors, "service " + service.Name, "Service contains a class method " + invalidMethod.Name);
                    CheckForPropertyMethod (errors, "service " + service.Name, serviceType);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError(errors, Context (Reflection.GetAttribute<KRPCServiceAttribute> (serviceType).Name ?? serviceType.Name), exn);
                }
            }

            // Scan for classes annotated with KRPCClass
            foreach (var classType in Reflection.GetTypesWith<KRPCClassAttribute> ()) {
                try {
                    CurrentAssembly = classType.Assembly;
                    TypeUtils.ValidateKRPCClass (classType);
                    var serviceName = TypeUtils.GetClassServiceName (classType);
                    if (!signatures.ContainsKey (serviceName)) {
                        HandleError(errors, "service " + serviceName, "Class " + serviceName + "." + classType.Name + " is unavailable, as its service does not exist");
                        continue;
                    }
                    var service = signatures [serviceName];
                    var cls = service.AddClass (classType);
                    // Add class methods
                    foreach (var method in Reflection.GetMethodsWith<KRPCMethodAttribute> (classType)) {
                        try {
                            service.AddClassMethod (cls, classType, method);
                        } catch (Exception exn) when (IsScanError (exn)) {
                            HandleError(errors, "service " + serviceName + ", class " + cls, exn);
                        }
                    }
                    // Add class properties
                    foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (classType)) {
                        try {
                            service.AddClassProperty (cls, classType, property);
                        } catch (Exception exn) when (IsScanError (exn)) {
                            HandleError(errors, "service " + serviceName + ", class " + cls, exn);
                        }
                    }
                    CheckForPropertyMethod (errors, "service " + serviceName + ", class " + cls, classType);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError(errors, TypeContext (classType, Reflection.GetAttribute<KRPCClassAttribute> (classType).Service), exn);
                }
            }

            // Scan for enumerations annotated with KRPCEnum
            foreach (var enumType in Reflection.GetTypesWith<KRPCEnumAttribute> ()) {
                try {
                    CurrentAssembly = enumType.Assembly;
                    TypeUtils.ValidateKRPCEnum (enumType);
                    var serviceName = TypeUtils.GetEnumServiceName (enumType);
                    if (!signatures.ContainsKey (serviceName)) {
                        HandleError(errors, "service " + serviceName, "Enumeration " + serviceName + "." + enumType.Name + " is unavailable, as its service does not exist");
                        continue;
                    }
                    var service = signatures [serviceName];
                    service.AddEnum (enumType);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError(errors, TypeContext (enumType, Reflection.GetAttribute<KRPCEnumAttribute> (enumType).Service), exn);
                }
            }

            // Scan for structures annotated with KRPCStruct. Every one found is validated,
            // including its fields, whether or not any procedure refers to it
            foreach (var structType in Reflection.GetTypesWith<KRPCStructAttribute> ()) {
                try {
                    CurrentAssembly = structType.Assembly;
                    TypeUtils.ValidateKRPCStruct (structType);
                    var serviceName = TypeUtils.GetStructServiceName (structType);
                    if (!signatures.ContainsKey (serviceName)) {
                        HandleError(errors, "service " + serviceName, "Struct " + serviceName + "." + structType.Name + " is unavailable, as its service does not exist");
                        continue;
                    }
                    var service = signatures [serviceName];
                    service.AddStruct (structType);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError(errors, TypeContext (structType, Reflection.GetAttribute<KRPCStructAttribute> (structType).Service), exn);
                }
            }

            // Scan for classes annotated with KRPCException
            foreach (var exnType in Reflection.GetTypesWith<KRPCExceptionAttribute> ()) {
                try {
                    CurrentAssembly = exnType.Assembly;
                    TypeUtils.ValidateKRPCException (exnType);
                    var serviceName = TypeUtils.GetExceptionServiceName (exnType);
                    if (!signatures.ContainsKey (serviceName)) {
                        HandleError(errors, "service " + serviceName, "Exception " + serviceName + "." + exnType.Name + " is unavailable, as its service does not exist");
                        continue;
                    }
                    var service = signatures [serviceName];
                    service.AddException (exnType);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError(errors, TypeContext (exnType, Reflection.GetAttribute<KRPCExceptionAttribute> (exnType).Service), exn);
                }
            }

            // Extension members are methods in public static classes that add a member to
            // another service's class. They are scanned last, once every class is registered
            var unreachable = new HashSet<Type> ();
            foreach (var method in Reflection.GetStaticClassMethodsWith<KRPCMethodAttribute> ())
                AddExtensionMember (signatures, errors, warnings, unreachable, method, false);
            foreach (var method in Reflection.GetStaticClassMethodsWith<KRPCPropertyAttribute> ())
                AddExtensionMember (signatures, errors, warnings, unreachable, method, true);

            CurrentAssembly = null;

            // Each check leaves out the members it reports. It runs over the members that the
            // checks before it kept, in a fixed order
            foreach (var name in declaredTwice)
                signatures.Remove (name);
            CheckServiceNames (signatures, errors);
            CheckServiceIds (signatures, errors);
            foreach (var service in signatures.Values) {
                service.RemoveDuplicates ();
                CheckTypes (service, errors);
                RemoveMembersOfMissingClasses (service);
                CheckProcedureIds (service, errors);
                CheckMemberNames (service, errors);
            }
            RemoveUnavailableMembers (signatures, errors);

            if (!signatures.ContainsKey ("KRPC"))
                throw new ServiceException ("KRPC service could not be found");
            return signatures;
        }

        /// <summary>
        /// Check that no two services share an id, and that none has an id of 0. Leaves out
        /// each service that breaks the rule.
        /// </summary>
        internal static void CheckServiceIds (IDictionary<string, ServiceSignature> services, IList<string> errors)
        {
            var removed = new List<string> ();
            foreach (var group in services.Values.GroupBy (x => x.Id).OrderBy (x => x.Key)) {
                var names = group.Select (x => x.Name).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (group.Key == 0) {
                    foreach (var name in names)
                        HandleError (errors, "service " + name, "Service has id 0");
                } else if (names.Count > 1) {
                    HandleError (errors, string.Empty,
                        "Services " + string.Join (", ", names.ToArray ()) + " share the id " + group.Key);
                } else {
                    continue;
                }
                removed.AddRange (names);
            }
            foreach (var name in removed)
                services.Remove (name);
        }

        /// <summary>
        /// Check that no two procedures in the service share an id, and that none has an id of 0.
        /// Leaves out each procedure that breaks the rule.
        /// </summary>
        internal static void CheckProcedureIds (ServiceSignature service, IList<string> errors)
        {
            var removed = new List<string> ();
            foreach (var group in service.Procedures.Values.GroupBy (x => x.Id).OrderBy (x => x.Key)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (group.Key == 0) {
                    foreach (var name in names)
                        HandleError (errors, "service " + service.Name, "Procedure " + name + " has id 0");
                } else if (names.Count > 1) {
                    HandleError (errors, "service " + service.Name,
                        "Procedures " + string.Join (", ", names.ToArray ()) + " share the id " + group.Key);
                } else {
                    continue;
                }
                removed.AddRange (group.Select (x => x.Name));
            }
            foreach (var name in removed)
                service.Procedures.Remove (name);
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
        /// Check that the services have unique names, compared case-insensitively. Leaves out
        /// each service that shares a name.
        /// </summary>
        internal static void CheckServiceNames (IDictionary<string, ServiceSignature> services, IList<string> errors)
        {
            var removed = new List<string> ();
            foreach (var group in services.Values.GroupBy (x => x.Name.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var names = group.Select (x => x.Name).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (names.Count > 1) {
                    HandleError (errors, string.Empty, "Services " + string.Join (", ", names.ToArray ()) + " share a name, compared case-insensitively");
                    removed.AddRange (names);
                }
            }
            foreach (var name in removed)
                services.Remove (name);
        }

        /// <summary>
        /// Check that no two members of a scope share a name, compared case-insensitively. The
        /// scopes are the service, each class, enumeration and structure, and the parameters of
        /// each procedure. A property also takes the names of its accessors, so a property Foo
        /// shares a name with a method GetFoo. Leaves out each member that shares a name, apart
        /// from a class's own member that shares one with an extension member. An enumeration
        /// or structure with a shared name is left out, and so is a procedure with one among
        /// its parameters.
        /// </summary>
        internal static void CheckMemberNames (ServiceSignature service, IList<string> errors)
        {
            var context = "service " + service.Name;
            // The names each member takes and its procedures, by scope and then by member
            var scopes = new Dictionary<string, Dictionary<string, KeyValuePair<string[], List<string>>>> ();
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
                Dictionary<string, KeyValuePair<string[], List<string>>> members;
                if (!scopes.TryGetValue (scope, out members))
                    scopes [scope] = members = new Dictionary<string, KeyValuePair<string[], List<string>>> ();
                // The getter and setter of a property are one member
                var kind = property ? " (property)" : parts [0] == "static" ? " (static)" : string.Empty;
                var key = scope + "." + member + kind;
                if (!members.ContainsKey (key))
                    members [key] = new KeyValuePair<string[], List<string>> (names, new List<string> ());
                members [key].Value.Add (procedure);
            }
            var removed = new HashSet<string> ();
            foreach (var members in scopes.OrderBy (x => x.Key, StringComparer.Ordinal).Select (x => x.Value)) {
                var clashes = CheckNames (members.SelectMany (x => x.Value.Key.Select (name => new KeyValuePair<string, string> (name, x.Key))), "Members", context, errors);
                foreach (var clash in clashes) {
                    var procedures = clash.Select (x => members [x].Value).ToList ();
                    // A class keeps its own member, and loses an extension member that clashes with
                    // it. Two of its own members that clash both go
                    var extensions = procedures.Where (x => x.Any (service.IsExtensionMember)).ToList ();
                    if (extensions.Count > 0 && procedures.Count - extensions.Count == 1)
                        procedures = extensions;
                    removed.UnionWith (procedures.SelectMany (x => x));
                }
            }
            var enumerations = new List<string> ();
            foreach (var enumeration in service.Enumerations.Values.OrderBy (x => x.Name, StringComparer.Ordinal)) {
                if (CheckNames (enumeration.Values.Select (x => new KeyValuePair<string, string> (x.Name, x.FullyQualifiedName)), "Values", context, errors).Any ())
                    enumerations.Add (enumeration.Name);
            }
            var structs = new List<string> ();
            foreach (var structure in service.Structs.Values.OrderBy (x => x.Name, StringComparer.Ordinal)) {
                if (CheckNames (structure.Fields.Select (x => new KeyValuePair<string, string> (x.Name, x.FullyQualifiedName)), "Fields", context, errors).Any ())
                    structs.Add (structure.Name);
            }
            foreach (var procedure in service.Procedures.Values.OrderBy (x => x.Name, StringComparer.Ordinal)) {
                if (CheckNames (procedure.Parameters.Select (x => new KeyValuePair<string, string> (x.Name, procedure.FullyQualifiedName + "(" + x.Name + ")")), "Parameters", context, errors).Any ())
                    removed.Add (procedure.Name);
            }
            foreach (var name in removed)
                service.Procedures.Remove (name);
            foreach (var name in enumerations)
                service.Enumerations.Remove (name);
            foreach (var name in structs)
                service.Structs.Remove (name);
        }

        /// <summary>
        /// Report each set of definitions that share a name, compared case-insensitively, given
        /// as pairs of a name and the definition that takes it. A set is reported once. Returns
        /// the sets.
        /// </summary>
        static IList<string[]> CheckNames (IEnumerable<KeyValuePair<string, string>> names, string kind, string context, IList<string> errors)
        {
            var reported = new HashSet<string> ();
            var clashes = new List<string[]> ();
            foreach (var group in names.GroupBy (x => x.Key.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var definitions = group.Select (x => x.Value).Distinct ().OrderBy (x => x, StringComparer.Ordinal).ToArray ();
                var list = string.Join (", ", definitions);
                if (definitions.Length > 1 && reported.Add (list)) {
                    HandleError (errors, context, kind + " " + list + " share the name " + group.Select (x => x.Key).OrderBy (x => x, StringComparer.Ordinal).First () + ", compared case-insensitively");
                    clashes.Add (definitions);
                }
            }
            return clashes;
        }

        /// <summary>
        /// Check that the classes, enumerations, structures and exceptions of the service have
        /// unique names, compared case-insensitively, none of them the name of the service, and
        /// unique ids, none of them 0. Leaves out each type that breaks a rule.
        /// </summary>
        internal static void CheckTypes (ServiceSignature service, IList<string> errors)
        {
            var types = service.Classes.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id, Types = (IDictionary)service.Classes })
                .Concat (service.Enumerations.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id, Types = (IDictionary)service.Enumerations }))
                .Concat (service.Structs.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id, Types = (IDictionary)service.Structs }))
                .Concat (service.Exceptions.Values.Select (x => new { x.Name, x.FullyQualifiedName, x.Id, Types = (IDictionary)service.Exceptions }))
                .ToList ();
            var context = "service " + service.Name;
            var removed = new HashSet<string> ();
            foreach (var type in types.Where (x => string.Equals (x.Name, service.Name, StringComparison.OrdinalIgnoreCase)).OrderBy (x => x.Name, StringComparer.Ordinal)) {
                HandleError (errors, context, "Type " + type.FullyQualifiedName + " has the name of its service, compared case-insensitively");
                removed.Add (type.FullyQualifiedName);
            }
            foreach (var group in types.GroupBy (x => x.Name.ToUpperInvariant ()).OrderBy (x => x.Key, StringComparer.Ordinal)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (names.Count > 1) {
                    HandleError (errors, context, "Types " + string.Join (", ", names.ToArray ()) + " share a name, compared case-insensitively");
                    removed.UnionWith (names);
                }
            }
            foreach (var group in types.GroupBy (x => x.Id).OrderBy (x => x.Key)) {
                var names = group.Select (x => x.FullyQualifiedName).OrderBy (x => x, StringComparer.Ordinal).ToList ();
                if (group.Key == 0) {
                    foreach (var name in names)
                        HandleError (errors, context, "Type " + name + " has id 0");
                    removed.UnionWith (names);
                } else if (names.Count > 1 && group.Select (x => x.Name).Distinct ().Count () > 1) {
                    // Types of one name share a hashed id, and are reported as sharing the name
                    HandleError (errors, context, "Types " + string.Join (", ", names.ToArray ()) + " share the id " + group.Key);
                    removed.UnionWith (names);
                }
            }
            foreach (var type in types.Where (x => removed.Contains (x.FullyQualifiedName)))
                type.Types.Remove (type.Name);
        }

        /// <summary>
        /// Leave out each member that uses a class, enumeration or structure that is not in a
        /// service, as its scan failed or a check left it out, and each member of a class that
        /// was left out. Leaving out a structure can leave out more members, so we repeat until
        /// nothing is left out.
        /// </summary>
        internal static void RemoveUnavailableMembers (IDictionary<string, ServiceSignature> services, IList<string> errors)
        {
            bool removed;
            do {
                var available = new HashSet<Type> ();
                foreach (var service in services.Values) {
                    available.UnionWith (service.Classes.Values.Select (x => x.UnderlyingType));
                    available.UnionWith (service.Enumerations.Values.Select (x => x.UnderlyingType));
                    available.UnionWith (service.Structs.Values.Select (x => x.UnderlyingType));
                }
                removed = false;
                foreach (var service in services.Values.OrderBy (x => x.Name, StringComparer.Ordinal)) {
                    var context = "service " + service.Name;
                    foreach (var structure in service.Structs.Values.OrderBy (x => x.Name, StringComparer.Ordinal).ToList ()) {
                        var field = structure.Fields.FirstOrDefault (x => FindUnavailable (x.Spec, available) != null);
                        if (field == null)
                            continue;
                        service.Structs.Remove (structure.Name);
                        HandleError (errors, context,
                            "Struct " + structure.FullyQualifiedName + " is unavailable, as its field " + field.Name +
                            " uses " + FindUnavailable (field.Spec, available) + ", which is unavailable");
                        removed = true;
                    }
                    RemoveMembersOfMissingClasses (service);
                    foreach (var procedure in service.Procedures.Values.OrderBy (x => x.Name, StringComparer.Ordinal).ToList ()) {
                        var specs = procedure.Parameters.Select (x => x.Spec);
                        if (procedure.HasReturnType)
                            specs = specs.Concat (new [] { procedure.ReturnSpec });
                        var type = specs.Select (x => FindUnavailable (x, available)).FirstOrDefault (x => x != null);
                        if (type == null)
                            continue;
                        service.Procedures.Remove (procedure.Name);
                        HandleError (errors, context,
                            "Procedure " + procedure.FullyQualifiedName + " is unavailable, as it uses " + type + ", which is unavailable");
                    }
                }
            } while (removed);
        }

        /// <summary>
        /// Leave out the members of each class that is not in the service. The error that left
        /// out the class names it, so they add none.
        /// </summary>
        internal static void RemoveMembersOfMissingClasses (ServiceSignature service)
        {
            foreach (var name in service.Procedures.Keys.ToList ()) {
                var parts = name.Split ('_');
                if (parts.Length > 1 && parts [0] != "get" && parts [0] != "set" && !service.Classes.ContainsKey (parts [0]))
                    service.Procedures.Remove (name);
            }
        }

        // The first class, enumeration or structure in the spec that is not available, or null for none
        static Type FindUnavailable (TypeSpec spec, ICollection<Type> available)
        {
            switch (spec.Kind) {
            case TypeKind.Class:
            case TypeKind.Enum:
            case TypeKind.Struct:
                return available.Contains (spec.Type) ? null : spec.Type;
            default:
                return spec.Types.Select (x => FindUnavailable (x, available)).FirstOrDefault (x => x != null);
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
                                        IList<string> warnings, ISet<Type> unreachable, MethodInfo method, bool isProperty)
        {
            var declaringType = method.DeclaringType;
            // Members declared in a service are handled when scanning the service itself
            if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                return;
            // A class that is not public is out of reach of the assembly the server runs from,
            // so its members are named in a warning
            if (!(declaringType.IsPublic || declaringType.IsNestedPublic)) {
                if (unreachable.Add (declaringType))
                    HandleWarning (errors, warnings, "Ignoring the extension members of " + declaringType + ", as the class is not public");
                return;
            }
            try {
                CurrentAssembly = declaringType.Assembly;
                var classType = TypeUtils.GetExtensionTargetClass (method);
                var serviceName = TypeUtils.GetClassServiceName (classType);
                if (!signatures.ContainsKey (serviceName)) {
                    HandleError (errors, "service " + serviceName,
                        "Extension member " + declaringType + "." + method.Name + " is unavailable, as its service does not exist");
                    return;
                }
                var service = signatures [serviceName];
                var cls = classType.Name;
                try {
                    if (isProperty)
                        service.AddClassExtensionProperty (cls, classType, method);
                    else
                        service.AddClassExtensionMethod (cls, classType, method);
                } catch (Exception exn) when (IsScanError (exn)) {
                    HandleError (errors, "service " + serviceName + ", class " + cls, exn);
                }
            } catch (Exception exn) when (IsScanError (exn)) {
                HandleError (errors, string.Empty, exn);
            }
        }

        // An error in one member: a broken rule, or a type or assembly its signature or
        // attributes name that fails to load
        static bool IsScanError (Exception exn)
        {
            return exn is ServiceException || exn is TypeLoadException || exn is FileNotFoundException ||
                exn is FileLoadException || exn is BadImageFormatException || exn is MissingMemberException ||
                exn is TypeInitializationException;
        }

                static string Context (string service)
        {
            return service == null ? string.Empty : "service " + service;
        }

        // The service a type names, for the context of its error: the one in its attribute, or
        // the service it is nested in
        static string TypeContext (Type type, string service)
        {
            var parent = type.DeclaringType;
            if (service == null && parent != null && Reflection.HasAttribute<KRPCServiceAttribute> (parent))
                service = Reflection.GetAttribute<KRPCServiceAttribute> (parent).Name ?? parent.Name;
            return Context (service);
        }

        static void HandleWarning (IList<string> errors, IList<string> warnings, string msg)
        {
            if (warnings != null)
                warnings.Add (msg);
            else
                HandleError (errors, new ServiceException (msg));
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
        /// Get mapping from exception types to kRPC exception types, from every valid kRPC
        /// exception. The scan reports the ones that are not valid.
        /// </summary>
        public static IDictionary<Type, Type> GetMappedExceptionTypes()
        {
            var exceptions = new List<Type> ();
            foreach (var exnType in Reflection.GetTypesWith<KRPCExceptionAttribute> ()) {
                try {
                    TypeUtils.ValidateKRPCException (exnType);
                    exceptions.Add (exnType);
                } catch (ServiceException) {
                }
            }
            return GetMappedExceptionTypes (exceptions);
        }

        /// <summary>
        /// Get mapping from exception types to the kRPC exception types of the given services.
        /// </summary>
        internal static IDictionary<Type, Type> GetMappedExceptionTypes (IEnumerable<ServiceSignature> services)
        {
            return GetMappedExceptionTypes (services.SelectMany (x => x.Exceptions.Values).Select (x => x.UnderlyingType));
        }

        // The first kRPC exception by full name takes a mapped type that several map
        static IDictionary<Type, Type> GetMappedExceptionTypes (IEnumerable<Type> exceptions)
        {
            IDictionary<Type, Type> mappedExceptionTypes = new Dictionary<Type, Type> ();
            foreach (var exnType in exceptions.OrderBy (x => x.FullName, StringComparer.Ordinal)) {
                var mappedExnType = Reflection.GetAttribute<KRPCExceptionAttribute> (exnType).MappedException;
                if (mappedExnType != null && !mappedExceptionTypes.ContainsKey (mappedExnType))
                    mappedExceptionTypes [mappedExnType] = exnType;
            }
            return mappedExceptionTypes;
        }
    }
}
