using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature of a kRPC service
    /// </summary>
    public sealed class ServiceSignature
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The id of the service
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// The static class that declares the service, or null for a service created by name.
        /// </summary>
        public Type UnderlyingType { get; private set; }

        /// <summary>
        /// Documentation for the service
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// A mapping from procedure names to signatures for all RPCs in this service
        /// </summary>
        public Dictionary<string,ProcedureSignature> Procedures { get; private set; }

        /// <summary>
        /// The classes defined in this service
        /// </summary>
        public Dictionary<string,ClassSignature> Classes { get; private set; }

        /// <summary>
        /// The enumerations defined in this service, and their allowed values
        /// </summary>
        public Dictionary<string,EnumerationSignature> Enumerations { get; private set; }

        /// <summary>
        /// The structures defined in this service, and their fields
        /// </summary>
        public Dictionary<string,StructSignature> Structs { get; private set; }

        /// <summary>
        /// The exceptions defined in this service
        /// </summary>
        public Dictionary<string,ExceptionSignature> Exceptions { get; private set; }

        /// <summary>
        /// Whether the service is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the service is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// The game scene of the service, to be inherited by procedures in the service.
        /// </summary>
        GameScene gameScene;

        /// <summary>
        /// The mod integrations the service requires, to be inherited by its procedures and
        /// classes, or null.
        /// </summary>
        ModRequirement requiredMod;

        /// <summary>
        /// The mod integrations each class requires, by class name.
        /// </summary>
        readonly Dictionary<string, ModRequirement> classRequiredMods = new Dictionary<string, ModRequirement> ();

        /// <summary>
        /// The type declaring each extension member added to this service, by procedure
        /// name, used when reporting a clash.
        /// </summary>
        readonly Dictionary<string,Type> extensionMembers = new Dictionary<string, Type> ();

        // The names declared more than once, by kind, left out by RemoveDuplicates
        readonly HashSet<string> duplicateProcedures = new HashSet<string> ();
        readonly HashSet<string> duplicateClasses = new HashSet<string> ();
        readonly HashSet<string> duplicateEnumerations = new HashSet<string> ();
        readonly HashSet<string> duplicateStructs = new HashSet<string> ();
        readonly HashSet<string> duplicateExceptions = new HashSet<string> ();

        /// <summary>
        /// Create a service signature from a C# type annotated with the KRPCService attribute
        /// </summary>
        public ServiceSignature (Type type, uint id)
        {
            TypeUtils.ValidateKRPCService (type);
            Name = TypeUtils.GetServiceName (type);
            Id = id;
            UnderlyingType = type;
            Documentation = DocumentationUtils.ResolveCrefs (type.GetDocumentation ());
            Classes = new Dictionary<string, ClassSignature> ();
            Enumerations = new Dictionary<string, EnumerationSignature> ();
            Structs = new Dictionary<string, StructSignature> ();
            Procedures = new Dictionary<string, ProcedureSignature> ();
            Exceptions = new Dictionary<string, ExceptionSignature> ();
            gameScene = TypeUtils.GetServiceGameScene (type);
            requiredMod = TypeUtils.GetRequiredMod (type, null);
            string deprecatedReason;
            Deprecated = TypeUtils.GetDeprecated (type, out deprecatedReason);
            DeprecatedReason = deprecatedReason;
        }

        /// <summary>
        /// Create a service with the given name.
        /// </summary>
        public ServiceSignature (string name, uint id)
        {
            TypeUtils.ValidateIdentifier (name);
            Name = name;
            Id = id;
            Documentation = string.Empty;
            Classes = new Dictionary<string, ClassSignature> ();
            Enumerations = new Dictionary<string, EnumerationSignature> ();
            Structs = new Dictionary<string, StructSignature> ();
            Procedures = new Dictionary<string, ProcedureSignature> ();
            Exceptions = new Dictionary<string, ExceptionSignature> ();
            gameScene = GameScene.All;
            Deprecated = false;
            DeprecatedReason = string.Empty;
        }

        /// <summary>
        /// Whether the procedure is an extension member, declared outside the class it extends.
        /// </summary>
        internal bool IsExtensionMember (string procedureName)
        {
            return extensionMembers.ContainsKey (procedureName);
        }

        /// <summary>
        /// Leave out every member whose name was declared more than once, the one added first
        /// included, so the outcome does not depend on the order of the scan. Adding the second
        /// one threw the error that reports it.
        /// </summary>
        internal void RemoveDuplicates ()
        {
            RemoveDuplicates (Procedures, duplicateProcedures);
            RemoveDuplicates (Classes, duplicateClasses);
            RemoveDuplicates (Enumerations, duplicateEnumerations);
            RemoveDuplicates (Structs, duplicateStructs);
            RemoveDuplicates (Exceptions, duplicateExceptions);
        }

        static void RemoveDuplicates<T> (IDictionary<string, T> members, ICollection<string> duplicates)
        {
            foreach (var name in duplicates)
                members.Remove (name);
            duplicates.Clear ();
        }

        /// <summary>
        /// Add a procedure to the service
        /// </summary>
        void AddProcedure (ProcedureSignature signature)
        {
            if (Procedures.ContainsKey (signature.Name)) {
                duplicateProcedures.Add (signature.Name);
                throw new ServiceException ("Procedure " + Name + "." + signature.Name + " is declared more than once, so each copy is unavailable");
            }
            Procedures [signature.Name] = signature;
        }

        /// <summary>
        /// Add a procedure to the service for the given method annotated with the KRPCProcedure attribute.
        /// </summary>
        public void AddProcedure (MethodInfo method)
        {
            TypeUtils.ValidateKRPCProcedure (method);
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (method, out deprecatedReason);
            AddProcedure (new ProcedureSignature (
                Name, method.Name, TypeUtils.GetMethodId (method), method.GetDocumentation (),
                new ProcedureHandler (method, TypeUtils.GetNullable (method), TypeUtils.GetNullablePaths (method)),
                TypeUtils.GetProcedureGameScene(method, gameScene),
                deprecated, deprecatedReason,
                TypeUtils.GetProcedureInternal (method),
                TypeUtils.GetRequiredMod (method, requiredMod)));
        }

        /// <summary>
        /// Add a property to the service for the given property annotated with the KRPCProperty attribute.
        /// </summary>
        public void AddProperty (PropertyInfo property)
        {
            TypeUtils.ValidateKRPCProperty (property);
            var getter = property.GetGetMethod ();
            var setter = property.GetSetMethod ();
            var nullable = TypeUtils.GetNullable (property);
            var nullablePaths = TypeUtils.GetNullablePaths (property).ToList ();
            if (getter != null)
                AddPropertyProcedure (property, getter, nullable, nullablePaths, false);
            if (setter != null)
                AddPropertyProcedure (property, setter, nullable, nullablePaths, true);
        }

        void AddPropertyProcedure (
            PropertyInfo property, MethodInfo method, bool nullable,
            IList<IList<Position>> nullablePaths, bool isSetter)
        {
            // For a getter the nullable flag and the paths apply to the return value. For a
            // setter they apply to the synthesized value parameter, which cannot carry the
            // attribute itself
            var handler = new ProcedureHandler (
                method, isSetter ? false : nullable,
                isSetter ? new List<IList<Position>> () : nullablePaths);
            if (isSetter)
                handler.SetValueParameterNullability (nullable, nullablePaths);
            string deprecatedReason;
            var deprecated = TypeUtils.GetPropertyDeprecated (property, method, out deprecatedReason);
            AddProcedure (new ProcedureSignature (
                Name, method.Name, TypeUtils.GetPropertyId (property, isSetter), property.GetDocumentation (), handler,
                TypeUtils.GetPropertyGameScene(property, gameScene),
                deprecated, deprecatedReason,
                requiredMod: GetPropertyRequiredMod (property)));
        }

        /// <summary>
        /// The mod integrations a property of the service requires. The Available property
        /// reports whether the mod is present, so it is exempt.
        /// </summary>
        ModRequirement GetPropertyRequiredMod (PropertyInfo property)
        {
            if (property.Name == "Available" && !Reflection.GetAttributes<RequiresModAttribute> (property).Any ())
                return null;
            return TypeUtils.GetPropertyRequiredMod (property, requiredMod);
        }

        /// <summary>
        /// Add a class to the service for the given class type annotated with the KRPCClass attribute.
        /// Returns the name of the class.
        /// </summary>
        public string AddClass (Type classType)
        {
            TypeUtils.ValidateKRPCClass (classType);
            TypeUtils.ValidateKRPCProcedureIds (classType);
            var name = classType.Name;
            if (Classes.ContainsKey (name)) {
                duplicateClasses.Add (name);
                throw new ServiceException ("Class " + Name + "." + name + " is declared more than once, so each copy is unavailable");
            }
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (classType, out deprecatedReason);
            classRequiredMods [name] = TypeUtils.GetRequiredMod (classType, requiredMod);
            Classes [name] = new ClassSignature (Name, name, classType, classType.GetDocumentation (), deprecated, deprecatedReason);
            return name;
        }

        /// <summary>
        /// Add an enum to the service for the given enum type annotated with the KRPCEnum attribute.
        /// Returns the name of the enumeration.
        /// </summary>
        public string AddEnum (Type enumType)
        {
            TypeUtils.ValidateKRPCEnum (enumType);
            var name = enumType.Name;
            if (Enumerations.ContainsKey (name)) {
                duplicateEnumerations.Add (name);
                throw new ServiceException ("Enumeration " + Name + "." + name + " is declared more than once, so each copy is unavailable");
            }
            var values = new List<EnumerationValueSignature> ();
            foreach (FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy (x => x.MetadataToken)) {
                string valueDeprecatedReason;
                var valueDeprecated = TypeUtils.GetDeprecated (field, out valueDeprecatedReason);
                values.Add (new EnumerationValueSignature (Name, name, field.Name, (int)field.GetRawConstantValue (), field.GetDocumentation (), valueDeprecated, valueDeprecatedReason));
            }
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (enumType, out deprecatedReason);
            Enumerations [name] = new EnumerationSignature (Name, name, enumType, values, enumType.GetDocumentation (), deprecated, deprecatedReason);
            return name;
        }

        /// <summary>
        /// Add a structure to the service for the given struct type annotated with the KRPCStruct attribute.
        /// Returns the name of the structure.
        /// </summary>
        public string AddStruct (Type structType)
        {
            TypeUtils.ValidateKRPCStruct (structType);
            var name = structType.Name;
            if (Structs.ContainsKey (name)) {
                duplicateStructs.Add (name);
                throw new ServiceException ("Struct " + Name + "." + name + " is declared more than once, so each copy is unavailable");
            }
            var fields = new List<StructFieldSignature> ();
            foreach (var property in TypeUtils.GetStructFields (structType)) {
                string fieldDeprecatedReason;
                var fieldDeprecated = TypeUtils.GetPropertyDeprecated (property, property.GetGetMethod (), out fieldDeprecatedReason);
                fields.Add (new StructFieldSignature (
                    Name, name, property.Name, TypeUtils.GetStructFieldSpec (structType, property),
                    property.GetDocumentation (), fieldDeprecated, fieldDeprecatedReason));
            }
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (structType, out deprecatedReason);
            Structs [name] = new StructSignature (Name, name, structType, fields, structType.GetDocumentation (), deprecated, deprecatedReason);
            return name;
        }

        /// <summary>
        /// Add an exception to the service for the given exception type annotated with the KRPCException attribute.
        /// Returns the name of the exception.
        /// </summary>
        public string AddException (Type exnType)
        {
            TypeUtils.ValidateKRPCException (exnType);
            var name = exnType.Name;
            if (Exceptions.ContainsKey (name)) {
                duplicateExceptions.Add (name);
                throw new ServiceException ("Exception " + Name + "." + name + " is declared more than once, so each copy is unavailable");
            }
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (exnType, out deprecatedReason);
            Exceptions [name] = new ExceptionSignature (Name, name, exnType, exnType.GetDocumentation (), deprecated, deprecatedReason);
            return name;
        }

        /// <summary>
        /// Add a class method to the given class in the given service for the given class type annotated with the KRPCClass attribute.
        /// </summary>
        public void AddClassMethod (string cls, Type classType, MethodInfo method)
        {
            TypeUtils.ValidateIdentifier (cls);
            TypeUtils.ValidateKRPCMethod (classType, method);
            if (!Classes.ContainsKey (cls))
                throw new ArgumentException ("Class " + cls + " does not exist");
            var name = method.Name;
            var id = TypeUtils.GetClassMethodId (classType, method);
            var classGameScene = TypeUtils.GetClassGameScene(classType, gameScene);
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (method, out deprecatedReason);
            if (!method.IsStatic) {
                var handler = new ClassMethodHandler (classType, method, TypeUtils.GetNullable (method), TypeUtils.GetNullablePaths (method));
                AddProcedure (new ProcedureSignature (
                    Name, cls + '_' + name, id, method.GetDocumentation (), handler,
                    TypeUtils.GetMethodGameScene(classType, method, classGameScene),
                    deprecated, deprecatedReason,
                    requiredMod: TypeUtils.GetRequiredMod (method, classRequiredMods [cls])));
            } else {
                var handler = new ClassStaticMethodHandler (method, TypeUtils.GetNullable (method), TypeUtils.GetNullablePaths (method));
                AddProcedure (new ProcedureSignature (
                    Name, cls + "_static_" + name, id, method.GetDocumentation (), handler,
                    TypeUtils.GetMethodGameScene(classType, method, classGameScene),
                    deprecated, deprecatedReason,
                    requiredMod: TypeUtils.GetRequiredMod (method, classRequiredMods [cls])));
            }
        }

        /// <summary>
        /// Add a class property to the given class in the given service for the given property annotated with the KRPCProperty attribute.
        /// </summary>
        public void AddClassProperty (string cls, Type classType, PropertyInfo property)
        {
            TypeUtils.ValidateIdentifier (cls);
            TypeUtils.ValidateKRPCClassProperty (classType, property);
            if (!Classes.ContainsKey (cls))
                throw new ArgumentException ("Class " + cls + " does not exist");
            var getter = property.GetGetMethod ();
            var setter = property.GetSetMethod ();
            var nullable = TypeUtils.GetNullable (property);
            var nullablePaths = TypeUtils.GetNullablePaths (property).ToList ();
            if (getter != null)
                AddClassPropertyMethod (cls, classType, property, getter, nullable, nullablePaths, false);
            if (setter != null)
                AddClassPropertyMethod (cls, classType, property, setter, nullable, nullablePaths, true);
        }

        void AddClassPropertyMethod (
            string cls, Type classType, PropertyInfo property, MethodInfo method, bool nullable,
            IList<IList<Position>> nullablePaths, bool isSetter)
        {
            // For a getter the nullable flag and the paths apply to the return value. For a
            // setter they apply to the synthesized value parameter, which cannot carry the
            // attribute itself
            var handler = new ClassMethodHandler (
                classType, method, isSetter ? false : nullable,
                isSetter ? new List<IList<Position>> () : nullablePaths);
            if (isSetter)
                handler.SetValueParameterNullability (nullable, nullablePaths);
            var classGameScene = TypeUtils.GetClassGameScene(classType, gameScene);
            string deprecatedReason;
            var deprecated = TypeUtils.GetPropertyDeprecated (property, method, out deprecatedReason);
            AddProcedure (new ProcedureSignature (
                Name, cls + '_' + method.Name, TypeUtils.GetClassPropertyId (classType, property, isSetter),
                property.GetDocumentation (), handler,
                TypeUtils.GetClassPropertyGameScene(classType, property, classGameScene),
                deprecated, deprecatedReason,
                requiredMod: TypeUtils.GetPropertyRequiredMod (property, classRequiredMods [cls])));
        }

        /// <summary>
        /// Add an extension method to the given class in the given service for the given
        /// method annotated with the KRPCMethod attribute.
        /// </summary>
        public void AddClassExtensionMethod (string cls, Type classType, MethodInfo method)
        {
            TypeUtils.ValidateIdentifier (cls);
            TypeUtils.ValidateKRPCExtensionMethod (method);
            CheckClassExists (cls);
            var classGameScene = TypeUtils.GetClassGameScene (classType, gameScene);
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (method, out deprecatedReason);
            AddExtensionProcedure (
                cls + '_' + method.Name, TypeUtils.GetMethodId (method), method, method.GetDocumentation (),
                new ClassStaticMethodHandler (method, TypeUtils.GetNullable (method), TypeUtils.GetNullablePaths (method), true),
                TypeUtils.GetExtensionMethodGameScene (method, classGameScene),
                deprecated, deprecatedReason, GetExtensionRequiredMod (cls, method));
        }

        /// <summary>
        /// Add one accessor of an extension property to the given class in the given service
        /// for the given method annotated with the KRPCProperty attribute.
        /// </summary>
        public void AddClassExtensionProperty (string cls, Type classType, MethodInfo method)
        {
            TypeUtils.ValidateIdentifier (cls);
            TypeUtils.ValidateKRPCExtensionProperty (method);
            CheckClassExists (cls);
            var isSetter = TypeUtils.IsAnExtensionPropertySetter (method);
            var property = TypeUtils.GetExtensionPropertyName (method);
            CheckOtherAccessor (cls + (isSetter ? "_get_" : "_set_") + property, method);
            var classGameScene = TypeUtils.GetClassGameScene (classType, gameScene);
            string deprecatedReason;
            var deprecated = TypeUtils.GetDeprecated (method, out deprecatedReason);
            AddExtensionProcedure (
                cls + (isSetter ? "_set_" : "_get_") + property, TypeUtils.GetPropertyId (method, isSetter),
                method, method.GetDocumentation (),
                new ClassStaticMethodHandler (method, TypeUtils.GetNullable (method), TypeUtils.GetNullablePaths (method), true),
                TypeUtils.GetExtensionPropertyGameScene (method, classGameScene),
                deprecated, deprecatedReason, GetExtensionRequiredMod (cls, method));
        }

        /// <summary>
        /// The mod integrations an extension member requires: from the member, then the class
        /// that declares it, then the class it extends.
        /// </summary>
        ModRequirement GetExtensionRequiredMod (string cls, MethodInfo method)
        {
            return TypeUtils.GetRequiredMod (
                method, TypeUtils.GetRequiredMod (method.DeclaringType, classRequiredMods [cls]));
        }

        void CheckClassExists (string cls)
        {
            if (!Classes.ContainsKey (cls))
                throw new ServiceException ("Class " + cls + " does not exist");
        }

        /// <summary>
        /// Check that the other accessor of an extension property, if the class has one, comes
        /// from the same extension. An accessor cannot be added to a property declared elsewhere.
        /// </summary>
        void CheckOtherAccessor (string procedureName, MethodInfo method)
        {
            if (!Procedures.ContainsKey (procedureName))
                return;
            Type owner;
            if (extensionMembers.TryGetValue (procedureName, out owner) && owner == method.DeclaringType)
                return;
            throw Clash (procedureName, method);
        }

        /// <summary>
        /// The error for an extension member that clashes with a procedure of the class. A class
        /// keeps its own member. Two extension members are both left out, as neither takes
        /// precedence.
        /// </summary>
        ServiceException Clash (string procedureName, MethodInfo method)
        {
            if (extensionMembers.ContainsKey (procedureName)) {
                duplicateProcedures.Add (procedureName);
                // An accessor takes the other accessor of its property with it
                if (procedureName.Contains ("_get_"))
                    duplicateProcedures.Add (procedureName.Replace ("_get_", "_set_"));
                else if (procedureName.Contains ("_set_"))
                    duplicateProcedures.Add (procedureName.Replace ("_set_", "_get_"));
            }
            Type owner;
            var declaredBy = extensionMembers.TryGetValue (procedureName, out owner) ? owner.ToString () : "the class itself";
            return new ServiceException (
                "Extension member " + method.DeclaringType + "." + method.Name + " clashes with " +
                Name + "." + procedureName + ", which is declared by " + declaredBy);
        }

        void AddExtensionProcedure (string procedureName, uint id, MethodInfo method, string documentation,
                                    IProcedureHandler handler, GameScene procedureGameScene,
                                    bool deprecated, string deprecatedReason, ModRequirement procedureRequiredMod)
        {
            if (Procedures.ContainsKey (procedureName))
                throw Clash (procedureName, method);
            AddProcedure (new ProcedureSignature (
                Name, procedureName, id, documentation, handler,
                procedureGameScene, deprecated, deprecatedReason,
                requiredMod: procedureRequiredMod));
            extensionMembers [procedureName] = method.DeclaringType;
        }

        // Sorted by name, as neither a Dictionary nor the reflection that fills it has a specified order
        static SortedDictionary<string, OrderedDictionary> SerializeSorted<T> (
            IDictionary<string, T> items, Func<T, OrderedDictionary> serialize)
        {
            var result = new SortedDictionary<string, OrderedDictionary> (StringComparer.Ordinal);
            foreach (var item in items)
                result [item.Key] = serialize (item.Value);
            return result;
        }

        /// <summary>
        /// The values of the signature in a service definition.
        /// </summary>
        public OrderedDictionary Serialize ()
        {
            var result = new OrderedDictionary ();
            result ["id"] = Id;
            result ["documentation"] = Documentation;
            result ["procedures"] = SerializeSorted (Procedures, x => x.Serialize ());
            result ["classes"] = SerializeSorted (Classes, x => x.Serialize ());
            result ["enumerations"] = SerializeSorted (Enumerations, x => x.Serialize ());
            result ["structs"] = SerializeSorted (Structs, x => x.Serialize ());
            result ["exceptions"] = SerializeSorted (Exceptions, x => x.Serialize ());
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            return result;
        }
    }
}
