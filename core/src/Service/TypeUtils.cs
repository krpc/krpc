using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KRPC.Service.Attributes;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Service
{
    static class TypeUtils
    {
        static Regex identifierPattern = new Regex ("^[A-Z][A-Za-z0-9]*\\z");
        static Regex namePattern = new Regex ("^[A-Za-z][A-Za-z0-9]*\\z");

        /// <summary>
        /// Returns true if the given identifier is a valid kRPC identifier.
        /// A valid identifier is a non-zero length string, containing ASCII letters and numbers,
        /// starting with an uppercase letter.
        /// </summary>
        public static bool IsAValidIdentifier (string identifier)
        {
            return identifierPattern.IsMatch (identifier);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC type.
        /// </summary>
        public static bool IsAValidType (Type type)
        {
            // A Nullable<T> is valid wherever T is. It is T at a position that allows a null,
            // which is how a collection declares a nullable element
            type = System.Nullable.GetUnderlyingType (type) ?? type;
            return IsAValueType (type) || IsAMessageType (type) || IsAClassType (type) || IsAnEnumType (type) || IsAStructType (type) || IsACollectionType (type);
        }

        /// <summary>
        /// Returns true if a value of the given type can be null. A value type holds one only
        /// as a Nullable&lt;T&gt;.
        /// </summary>
        public static bool CanBeNull (Type type)
        {
            return !type.IsValueType || System.Nullable.GetUnderlyingType (type) != null;
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC key type in dictionaries.
        /// </summary>
        public static bool IsAValidKeyType (Type type)
        {
            return
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(uint) ||
            type == typeof(ulong) ||
            type == typeof(bool) ||
            type == typeof(string);
        }

        /// <summary>
        /// Returns true if the given type is a kRPC value type.
        /// </summary>
        public static bool IsAValueType (Type type)
        {
            return
            type == typeof(double) ||
            type == typeof(float) ||
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(uint) ||
            type == typeof(ulong) ||
            type == typeof(bool) ||
            type == typeof(string) ||
            type == typeof(byte[]);
        }

        /// <summary>
        /// Returns true if the given type is a kRPC message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            return typeof(IMessage).IsAssignableFrom (type);
        }

        /// <summary>
        /// Returns true if a value of the given type holds a stream or an event, at any position.
        /// </summary>
        public static bool HoldsAStream (Type type)
        {
            if (type == typeof(Messages.Stream) || type == typeof(Messages.Event))
                return true;
            return type.IsGenericType && type.GetGenericArguments ().Any (HoldsAStream);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC class type.
        /// </summary>
        public static bool IsAClassType (ICustomAttributeProvider type)
        {
            return Reflection.HasAttribute<KRPCClassAttribute> (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC enum type.
        /// </summary>
        public static bool IsAnEnumType (Type type)
        {
            return Reflection.HasAttribute<KRPCEnumAttribute> (type) && Enum.GetUnderlyingType (type) == typeof(int);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC structure type.
        /// </summary>
        public static bool IsAStructType (ICustomAttributeProvider type)
        {
            return Reflection.HasAttribute<KRPCStructAttribute> (type);
        }

        /// <summary>
        /// The properties that make up the fields of the given kRPC structure type, in the
        /// order they are declared in. The fields are the public instance properties
        /// annotated with the KRPCProperty attribute that have both a getter and a setter.
        ///
        /// The order is the order the wire encoding puts the field values in, so it has to
        /// be the same every time it is asked for, whichever assembly the structure is in.
        /// Reflection does not promise an order for the properties of a type, so they are
        /// ordered by metadata token, which is assigned in declaration order.
        /// </summary>
        public static IList<PropertyInfo> GetStructFields (Type type)
        {
            lock (structFields) {
                IList<PropertyInfo> fields;
                if (structFields.TryGetValue (type, out fields))
                    return fields;
                fields = Reflection.GetPropertiesWith<KRPCPropertyAttribute> (type)
                    .Where (property => property.IsPublic () && !property.IsStatic () &&
                            property.GetGetMethod () != null && property.GetSetMethod () != null)
                    .OrderBy (property => property.MetadataToken)
                    .ToList ();
                structFields [type] = fields;
                return fields;
            }
        }

        /// <summary>
        /// The fields of every structure type they have been asked for. Encoding a structure
        /// value needs them, which happens once per stream update per value.
        /// </summary>
        static readonly IDictionary<Type, IList<PropertyInfo>> structFields = new Dictionary<Type, IList<PropertyInfo>> ();

        /// <summary>
        /// The fields of every structure type they have been asked for, paired with their
        /// specs. Encoding a value reads both, so one entry holds both and answers with one
        /// lookup.
        /// </summary>
        static readonly IDictionary<Type, KeyValuePair<IList<PropertyInfo>, IList<TypeSpec>>> structFieldSpecs =
            new Dictionary<Type, KeyValuePair<IList<PropertyInfo>, IList<TypeSpec>>> ();

        /// <summary>
        /// Returns true if the given type can be used as a kRPC collection type.
        /// </summary>
        public static bool IsACollectionType (Type type)
        {
            return IsATupleCollectionType (type) || IsAListCollectionType (type) || IsASetCollectionType (type) || IsADictionaryCollectionType (type);
        }

        // The CLR tuple types are flat up to seven elements, and nest a tuple in the
        // eighth position beyond that, which is not a shape kRPC can carry
        public const int MaxTupleElements = 7;

        /// <summary>
        /// Returns true if the given type can be used as a kRPC tuple collection type.
        /// </summary>
        public static bool IsATupleCollectionType (Type type)
        {
            return type.Name.StartsWith("Tuple`", StringComparison.Ordinal) &&
            type.GetGenericArguments().All(IsAValidType);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC list collection type.
        /// </summary>
        public static bool IsAListCollectionType (Type type)
        {
            return Reflection.IsGenericType (type, typeof(IList<>)) &&
            type.GetGenericArguments ().Length == 1 &&
            IsAValidType (type.GetGenericArguments ().Single ());
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC list collection type.
        /// </summary>
        public static bool IsASetCollectionType (Type type)
        {
            return Reflection.IsGenericType (type, typeof(HashSet<>)) &&
            type.GetGenericArguments ().Length == 1 &&
            // A set element cannot be nullable. A set of them holds at most one null, which is
            // rarely wanted, and hashing one costs every client something
            System.Nullable.GetUnderlyingType (type.GetGenericArguments ().Single ()) == null &&
            IsAValidType (type.GetGenericArguments ().Single ());
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC dictionary collection type.
        /// </summary>
        public static bool IsADictionaryCollectionType (Type type)
        {
            return Reflection.IsGenericType (type, typeof(IDictionary<,>)) &&
            type.GetGenericArguments ().Length == 2 &&
            IsAValidKeyType (type.GetGenericArguments () [0]) &&
            IsAValidType (type.GetGenericArguments () [1]);
        }

        /// <summary>
        /// Get the name of the service for the given KRPCService annotated type
        /// </summary>
        public static string GetServiceName (Type type)
        {
            ValidateKRPCService (type);
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            var name = attribute.Name;
            return name ?? type.Name;
        }

        /// <summary>
        /// Get the id of the service for the given KRPCService annotated type
        /// </summary>
        public static uint GetServiceId (Type type)
        {
            ValidateKRPCService (type);
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            return GetId (GetServiceName (type), attribute.Id);
        }

        /// <summary>
        /// The given id, or if it is 0 the id generated from the given name: the first 4
        /// bytes of its SHA-256 hash, read as a little-endian integer.
        /// </summary>
        public static uint GetId (string name, uint id)
        {
            if (id != 0)
                return id;
            using (var sha256 = SHA256.Create ()) {
                var hash = sha256.ComputeHash (Encoding.UTF8.GetBytes (name));
                return hash [0] | (uint)hash [1] << 8 | (uint)hash [2] << 16 | (uint)hash [3] << 24;
            }
        }

        /// <summary>
        /// The id of the given class, enumeration, structure or exception type: the id set in
        /// its attribute, or the hash of its name.
        /// </summary>
        public static uint GetTypeId (Type type)
        {
            uint id;
            if (Reflection.HasAttribute<KRPCClassAttribute> (type))
                id = Reflection.GetAttribute<KRPCClassAttribute> (type).Id;
            else if (Reflection.HasAttribute<KRPCEnumAttribute> (type))
                id = Reflection.GetAttribute<KRPCEnumAttribute> (type).Id;
            else if (Reflection.HasAttribute<KRPCStructAttribute> (type))
                id = Reflection.GetAttribute<KRPCStructAttribute> (type).Id;
            else if (Reflection.HasAttribute<KRPCExceptionAttribute> (type))
                id = Reflection.GetAttribute<KRPCExceptionAttribute> (type).Id;
            else
                throw new ArgumentException (type + " is not a kRPC class, enumeration, structure or exception");
            return GetId (type.Name, id);
        }

        /// <summary>
        /// The id set in the attribute of the given procedure, method or extension method,
        /// or 0 for none.
        /// </summary>
        public static uint GetMethodId (MethodInfo method)
        {
            if (Reflection.HasAttribute<KRPCProcedureAttribute> (method))
                return Reflection.GetAttribute<KRPCProcedureAttribute> (method).Id;
            return Reflection.GetAttribute<KRPCMethodAttribute> (method).Id;
        }

        /// <summary>
        /// Whether the attribute of the given procedure marks it internal.
        /// </summary>
        public static bool GetProcedureInternal (MethodInfo method)
        {
            return Reflection.GetAttribute<KRPCProcedureAttribute> (method).Internal;
        }

        /// <summary>
        /// The id set in the attribute of the given property for its getter or setter, or
        /// 0 for none.
        /// </summary>
        public static uint GetPropertyId (ICustomAttributeProvider property, bool isSetter)
        {
            var attribute = Reflection.GetAttribute<KRPCPropertyAttribute> (property);
            return isSetter ? attribute.SetterId : attribute.GetterId;
        }

        /// <summary>
        /// The id set for a method of the given class, or 0 for none.
        /// </summary>
        public static uint GetClassMethodId (Type cls, MethodInfo method)
        {
            return GetClassMemberId (cls, method, GetMethodId (method), x => x.Id);
        }

        /// <summary>
        /// The id set for the getter or setter of a property of the given class, or 0 for none.
        /// </summary>
        public static uint GetClassPropertyId (Type cls, PropertyInfo property, bool isSetter)
        {
            return GetClassMemberId (cls, property, GetPropertyId (property, isSetter),
                                     x => isSetter ? x.SetterId : x.GetterId);
        }

        /// <summary>
        /// The id set for a member of the given class. The member's own attribute sets it when
        /// the class declares the member, and a KRPCProcedureId on the class when the class
        /// inherits it.
        /// </summary>
        static uint GetClassMemberId (Type cls, MemberInfo member, uint memberId,
                                      Func<KRPCProcedureIdAttribute, uint> inheritedId)
        {
            var declaringType = member.DeclaringType;
            if (declaringType == cls)
                return memberId;
            // An id on a member of a base KRPCClass belongs to the base's procedure
            if (memberId != 0 && !Reflection.HasAttribute<KRPCClassAttribute> (declaringType))
                throw new ServiceException (
                    (member is MethodInfo ? "KRPCMethod " : "KRPCProperty ") + declaringType + "." + member.Name +
                    " sets an id, but " + declaringType + " is not a KRPCClass; set the id on " + cls +
                    " with KRPCProcedureId");
            var attribute = Reflection.GetAttributes<KRPCProcedureIdAttribute> (cls)
                .FirstOrDefault (x => x.Member == member.Name);
            return attribute == null ? 0 : inheritedId (attribute);
        }

        /// <summary>
        /// Check that each KRPCProcedureId on the given class names a member the class inherits,
        /// once, and sets ids only for the procedures that member has. A base class that is not
        /// a KRPCClass cannot carry the attribute.
        /// </summary>
        public static void ValidateKRPCProcedureIds (Type cls)
        {
            for (var type = cls.BaseType; type != null; type = type.BaseType) {
                if (!Reflection.HasAttribute<KRPCClassAttribute> (type) &&
                    Reflection.GetAttributes<KRPCProcedureIdAttribute> (type).Any ())
                    throw new ServiceException (
                        "KRPCProcedureId is applied to " + type + ", a base of " + cls +
                        " that is not a KRPCClass; apply it to " + cls);
            }
            var methods = new HashSet<string> ();
            foreach (var method in Reflection.GetMethodsWith<KRPCMethodAttribute> (cls)) {
                if (method.DeclaringType != cls)
                    methods.Add (method.Name);
            }
            var properties = new Dictionary<string, PropertyInfo> ();
            foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (cls)) {
                if (property.DeclaringType != cls)
                    properties [property.Name] = property;
            }
            var named = new HashSet<string> ();
            foreach (var attribute in Reflection.GetAttributes<KRPCProcedureIdAttribute> (cls)) {
                var context = "KRPCProcedureId on " + cls + " for " + attribute.Member;
                PropertyInfo property;
                if (methods.Contains (attribute.Member)) {
                    if (attribute.GetterId != 0 || attribute.SetterId != 0)
                        throw new ServiceException (context + " sets GetterId or SetterId, but the member is a method");
                    if (attribute.Id == 0)
                        throw new ServiceException (context + " does not set Id");
                } else if (properties.TryGetValue (attribute.Member, out property)) {
                    if (attribute.Id != 0)
                        throw new ServiceException (context + " sets Id, but the member is a property");
                    if (attribute.GetterId == 0 && attribute.SetterId == 0)
                        throw new ServiceException (context + " sets neither GetterId nor SetterId");
                    ValidateAccessorIds (context, attribute.GetterId, attribute.SetterId,
                                         property.GetGetMethod () != null, property.GetSetMethod () != null);
                } else {
                    throw new ServiceException (context + " does not name a member the class inherits");
                }
                if (!named.Add (attribute.Member))
                    throw new ServiceException (context + " is applied more than once");
            }
        }

        /// <summary>
        /// Check that a property sets an id only for an accessor it has.
        /// </summary>
        static void ValidatePropertyIds (MemberInfo property, bool hasGetter, bool hasSetter)
        {
            var attribute = Reflection.GetAttribute<KRPCPropertyAttribute> (property);
            ValidateAccessorIds ("KRPCProperty " + property, attribute.GetterId, attribute.SetterId, hasGetter, hasSetter);
        }

        /// <summary>
        /// Check that ids are set only for the accessors a property has.
        /// </summary>
        static void ValidateAccessorIds (string context, uint getterId, uint setterId, bool hasGetter, bool hasSetter)
        {
            if (getterId != 0 && !hasGetter)
                throw new ServiceException (context + " sets GetterId, but has no getter");
            if (setterId != 0 && !hasSetter)
                throw new ServiceException (context + " sets SetterId, but has no setter");
        }

        /// <summary>
        /// Get the game scene(s) that the service should be available during
        /// </summary>
        public static GameScene GetServiceGameScene (Type type)
        {
            ValidateKRPCService (type);
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            if (attribute.GameScene == GameScene.Inherit)
                return GameScene.All;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that the procedure should be available during
        /// </summary>
        public static GameScene GetProcedureGameScene (MethodBase method, GameScene serviceGameScene)
        {
            ValidateKRPCProcedure (method);
            var attribute = Reflection.GetAttribute<KRPCProcedureAttribute> (method);
            if (attribute.GameScene == GameScene.Inherit)
                return serviceGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that the property should be available during
        /// </summary>
        public static GameScene GetPropertyGameScene (PropertyInfo property, GameScene serviceGameScene)
        {
            ValidateKRPCProperty (property);
            var attribute = Reflection.GetAttribute<KRPCPropertyAttribute> (property);
            if (attribute.GameScene == GameScene.Inherit)
                return serviceGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get whether the given member is deprecated, i.e. annotated with the standard
        /// [Obsolete] attribute. If it is, <paramref name="reason"/> is set to the
        /// attribute's message (empty when none was given). The message may contain
        /// see-cref markup; crefs are resolved relative to the declaring type.
        /// </summary>
        public static bool GetDeprecated (ICustomAttributeProvider member, out string reason)
        {
            if (Reflection.HasAttribute<ObsoleteAttribute> (member)) {
                reason = Reflection.GetAttribute<ObsoleteAttribute> (member).Message ?? string.Empty;
                reason = DocumentationUtils.ResolveDeprecationReason (reason, GetDeprecationContext (member));
                return true;
            }
            reason = string.Empty;
            return false;
        }

        static Type GetDeprecationContext (ICustomAttributeProvider member)
        {
            var type = member as Type;
            if (type != null)
                return type;
            var memberInfo = member as MemberInfo;
            return memberInfo == null ? null : memberInfo.DeclaringType;
        }

        /// <summary>
        /// Get whether the given property is deprecated. The [Obsolete] attribute is read
        /// from the property itself, falling back to the given accessor method.
        /// </summary>
        public static bool GetPropertyDeprecated (PropertyInfo property, MethodInfo accessor, out string reason)
        {
            if (Reflection.HasAttribute<ObsoleteAttribute> (property))
                return GetDeprecated (property, out reason);
            return GetDeprecated (accessor, out reason);
        }

        /// <summary>
        /// Get the name of the service for the given KRPCClass annotated type
        /// </summary>
        public static string GetClassServiceName (Type type)
        {
            ValidateKRPCClass (type);
            var attribute = Reflection.GetAttribute<KRPCClassAttribute> (type);
            return attribute.Service ?? GetServiceName (type.DeclaringType);
        }

        /// <summary>
        /// Get the game scene(s) that the class should be available during
        /// </summary>
        public static GameScene GetClassGameScene (Type type, GameScene serviceGameScene)
        {
            ValidateKRPCClass (type);
            var attribute = Reflection.GetAttribute<KRPCClassAttribute> (type);
            if (attribute.GameScene == GameScene.Inherit)
                return serviceGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that the class method should be available during
        /// </summary>
        public static GameScene GetMethodGameScene (Type cls, MethodBase method, GameScene classGameScene)
        {
            ValidateKRPCMethod (cls, method);
            var attribute = Reflection.GetAttribute<KRPCMethodAttribute> (method);
            if (attribute.GameScene == GameScene.Inherit)
                return classGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that the class property should be available during
        /// </summary>
        public static GameScene GetClassPropertyGameScene (Type cls, PropertyInfo property, GameScene serviceGameScene)
        {
            ValidateKRPCClassProperty (cls, property);
            var attribute = Reflection.GetAttribute<KRPCPropertyAttribute> (property);
            if (attribute.GameScene == GameScene.Inherit)
                return serviceGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that an extension method should be available during
        /// </summary>
        public static GameScene GetExtensionMethodGameScene (MethodInfo method, GameScene classGameScene)
        {
            ValidateKRPCExtensionMethod (method);
            var attribute = Reflection.GetAttribute<KRPCMethodAttribute> (method);
            if (attribute.GameScene == GameScene.Inherit)
                return classGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the game scene(s) that an extension property should be available during
        /// </summary>
        public static GameScene GetExtensionPropertyGameScene (MethodInfo method, GameScene classGameScene)
        {
            ValidateKRPCExtensionProperty (method);
            var attribute = Reflection.GetAttribute<KRPCPropertyAttribute> (method);
            if (attribute.GameScene == GameScene.Inherit)
                return classGameScene;
            return attribute.GameScene;
        }

        /// <summary>
        /// Get the mod integrations the given service, class or member requires, from its
        /// RequiresMod attributes, or <paramref name="inherited"/> if it has none.
        /// </summary>
        public static ModRequirement GetRequiredMod (ICustomAttributeProvider member, ModRequirement inherited)
        {
            var attributes = Reflection.GetAttributes<RequiresModAttribute> (member).ToList ();
            if (attributes.Count == 0)
                return inherited;
            var alternatives = new List<IList<ModAPI>> ();
            foreach (var attribute in attributes) {
                if (attribute.Types.Length == 0)
                    throw new ServiceException ("RequiresMod on " + member + " names no integration");
                alternatives.Add (attribute.Types.Select (type => GetModAPI (member, type)).ToList ().AsReadOnly ());
            }
            return new ModRequirement (alternatives.AsReadOnly ());
        }

        /// <summary>
        /// Get the mod integrations the given property requires, as
        /// <see cref="GetRequiredMod"/>. Throws an exception if an accessor of the property
        /// has a RequiresMod attribute, as only the property's own attributes apply.
        /// </summary>
        public static ModRequirement GetPropertyRequiredMod (PropertyInfo property, ModRequirement inherited)
        {
            foreach (var accessor in property.GetAccessors (true)) {
                if (Reflection.GetAttributes<RequiresModAttribute> (accessor).Any ())
                    throw new ServiceException (
                        "RequiresMod on an accessor of " + property.Name + ", in place of on the property");
            }
            return GetRequiredMod (property, inherited);
        }

        static ModAPI GetModAPI (ICustomAttributeProvider member, Type type)
        {
            if (type == null || type.IsAbstract || !type.IsSubclassOf (typeof (ModAPI)))
                throw new ServiceException (
                    "RequiresMod on " + member + " names " + (type == null ? "null" : type.ToString ()) +
                    ", which is not a subclass of " + typeof (ModAPI));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (type.GetConstructor (flags, null, Type.EmptyTypes, null) == null)
                throw new ServiceException (
                    "RequiresMod on " + member + " names " + type + ", which has no parameterless constructor");
            try {
                return ModAPI.Get (type);
            } catch (InvalidOperationException exn) {
                throw new ServiceException ("RequiresMod on " + member + " names " + type + ": " + exn.Message);
            }
        }

        /// <summary>
        /// Get the name of the service for the given KRPCEnum annotated type
        /// </summary>
        public static string GetEnumServiceName (Type type)
        {
            ValidateKRPCEnum (type);
            var attribute = Reflection.GetAttribute<KRPCEnumAttribute> (type);
            return attribute.Service ?? GetServiceName (type.DeclaringType);
        }

        /// <summary>
        /// Get the name of the service for the given KRPCStruct annotated type
        /// </summary>
        public static string GetStructServiceName (Type type)
        {
            ValidateKRPCStruct (type);
            var attribute = Reflection.GetAttribute<KRPCStructAttribute> (type);
            return attribute.Service ?? GetServiceName (type.DeclaringType);
        }

        /// <summary>
        /// Get the name of the service for the given KRPCException annotated type
        /// </summary>
        public static string GetExceptionServiceName (Type type)
        {
            ValidateKRPCException (type);
            var attribute = Reflection.GetAttribute<KRPCExceptionAttribute> (type);
            return attribute.Service ?? GetServiceName (type.DeclaringType);
        }

        /// <summary>
        /// Check if the string is a valid identifier for a kRPC service, procedure, property, class or method.
        /// </summary>
        public static void ValidateIdentifier (string name)
        {
            if (!IsAValidIdentifier (name))
                throw new ServiceException (name + " is not a valid kRPC identifier");
        }

        /// <summary>
        /// Check if the string is a valid name for a parameter or an enumeration value: ASCII
        /// letters and numbers, starting with a letter.
        /// </summary>
        public static void ValidateName (string name, string location)
        {
            if (name == null || !namePattern.IsMatch (name))
                throw new ServiceException ((name ?? "null") + " is not a valid kRPC name, for " + location);
        }

        /// <summary>
        /// Check the given type is a valid kRPC service
        /// 1. Must have KRPCService attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public static class
        /// 4. Must not be declared inside another kRPC service
        /// </summary>
        public static void ValidateKRPCService (Type type)
        {
            if (!Reflection.HasAttribute<KRPCServiceAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCService attribute");
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier. If Name is specified, use that as the identifier.
            ValidateIdentifier (attribute.Name ?? type.Name);
            // Check it's public static
            if (!((type.IsPublic || type.IsNestedPublic) && type.IsStatic ()))
                throw new ServiceException ("KRPCService " + type + " is not public static");
            // Check it's not nested inside another KRPCServiceAttribute
            var declaringType = type.DeclaringType;
            while (declaringType != null) {
                if (Reflection.HasAttribute<KRPCServiceAttribute> (type))
                    throw new ServiceException ("KRPCService " + type + " must not be declared inside another KRPCService");
                declaringType = declaringType.DeclaringType;
            }
        }

        /// <summary>
        /// Check the given method is a valid kRPC procedure
        /// 1. Must have KRPCProcedure attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public static method
        /// 4. Must be declared inside a kRPC service
        /// </summary>
        public static void ValidateKRPCProcedure (MethodBase method)
        {
            if (!Reflection.HasAttribute<KRPCProcedureAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCProcedure attribute");
            // Note: Type must already be a method, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (method.Name);
            // Check the method is public static
            if (!(method.IsPublic && method.IsStatic))
                throw new ServiceException ("KRPCProcedure " + method + " is not public static");
            // Check its defined directly in a KRPCServiceAttribute
            var declaringType = method.DeclaringType;
            if (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                throw new ServiceException ("KRPCProcedure " + method + " is not declared inside a KRPCService");
        }

        /// <summary>
        /// Check the given type is a valid kRPC property
        /// 1. Must have KRPCProperty attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public static property
        /// 4. Must be declared inside a kRPC service
        /// </summary>
        public static void ValidateKRPCProperty (PropertyInfo property)
        {
            if (!Reflection.HasAttribute<KRPCPropertyAttribute> (property))
                throw new ArgumentException (property + " does not have KRPCProperty attribute");
            // Note: Type must already be a property, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (property.Name);
            // Check it's public static
            if (!(property.IsPublic () && property.IsStatic ()))
                throw new ServiceException ("KRPCProperty " + property + " is not public static");
            // Check the method is defined directly inside a KRPCService
            var declaringType = property.DeclaringType;
            if (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                throw new ServiceException ("KRPCProperty " + property + " is not declared inside a KRPCService");
            ValidatePropertyIds (property, property.GetGetMethod () != null, property.GetSetMethod () != null);
        }

        /// <summary>
        /// Check the given type is a valid kRPC class
        /// 1. Must have KRPCClass attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public non-static class
        /// 4. Must be declared inside a kRPC service if it doesn't have the service explicity set
        /// 5. Must not be declared inside a kRPC service if it does have the service explicity set
        /// </summary>
        public static void ValidateKRPCClass (Type type)
        {
            if (!Reflection.HasAttribute<KRPCClassAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCClass attribute");
            var attribute = Reflection.GetAttribute<KRPCClassAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (type.Name);
            // Check it's public non-static
            if (!((type.IsPublic || type.IsNestedPublic) && !type.IsStatic ()))
                throw new ServiceException ("KRPCClass " + type + " is not public non-static");
            // If it doesn't have the Service property set, check the class is defined directly inside a KRPCService
            var declaringType = type.DeclaringType;
            if (attribute.Service == null && (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType)))
                throw new ServiceException ("KRPCClass " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the class isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCClass " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
        }

        /// <summary>
        /// Check the given type is a valid kRPC enumeration
        /// 1. Must have KRPCEnum attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public enum
        /// 4. Underlying type must be a 32-bit signed integer (int)
        /// 5. Must be declared inside a kRPC service if it doesn't have the service explicity set
        /// 6. Must not be declared inside a kRPC service if it does have the service explicity set
        /// 7. A flags enumeration must have values that are 0, a single bit, or a union of single
        ///    bits it declares, and no negative value
        /// </summary>
        public static void ValidateKRPCEnum (Type type)
        {
            if (!Reflection.HasAttribute<KRPCEnumAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCEnum attribute");
            var attribute = Reflection.GetAttribute<KRPCEnumAttribute> (type);
            // Note: Type must already be an enum, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (type.Name);
            // Check it's public
            if (!(type.IsPublic || type.IsNestedPublic))
                throw new ServiceException ("KRPCEnum " + type + " is not public");
            // Check the underlying type is an int
            if (Enum.GetUnderlyingType (type) != typeof(int))
                throw new ServiceException ("KRPCEnum " + type + " has underlying type " + Enum.GetUnderlyingType (type) + "; but only int is supported");
            if (Enumerations.IsFlags (type))
                Enumerations.ValidateFlags (type);
            // If it doesn't have the Service property set, check the enum is defined directly inside a KRPCService
            var declaringType = type.DeclaringType;
            if (attribute.Service == null && (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType)))
                throw new ServiceException ("KRPCEnum " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the class isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCClass " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
        }

        /// <summary>
        /// Check the given type is a valid kRPC structure
        /// 1. Must have KRPCStruct attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public non-generic value type
        /// 4. Must be declared inside a kRPC service if it doesn't have the service explicity set
        /// 5. Must not be declared inside a kRPC service if it does have the service explicity set
        /// 6. Must have valid fields, see <see cref="ValidateStructFields"/>
        /// </summary>
        public static void ValidateKRPCStruct (Type type)
        {
            if (!Reflection.HasAttribute<KRPCStructAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCStruct attribute");
            var attribute = Reflection.GetAttribute<KRPCStructAttribute> (type);
            // Note: Type must already be a struct, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (type.Name);
            // Check it's public and not generic
            if (!(type.IsPublic || type.IsNestedPublic))
                throw new ServiceException ("KRPCStruct " + type + " is not public");
            if (type.IsGenericType)
                throw new ServiceException ("KRPCStruct " + type + " is generic");
            // If it doesn't have the Service property set, check the struct is defined directly inside a KRPCService
            var declaringType = type.DeclaringType;
            if (attribute.Service == null && (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType)))
                throw new ServiceException ("KRPCStruct " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the struct isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCStruct " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
            ValidateStructFields (type);
        }

        /// <summary>
        /// Check the fields of the given structure type
        /// 1. Every property marked as a field must have a valid identifier
        /// 2. Every property marked as a field must be a public instance property with both a
        ///    getter and a setter, so that a value can be both read and constructed
        /// 3. Every field must have a valid kRPC type
        /// 4. A field must not be restricted to a game scene
        /// 5. There must be at least one field
        /// 6. The structure must not contain itself, directly or through the fields of another
        ///    structure
        /// </summary>
        public static void ValidateStructFields (Type type)
        {
            foreach (var property in Reflection.GetPropertiesWith<KRPCPropertyAttribute> (type)) {
                ValidateIdentifier (property.Name);
                if (!property.IsPublic () || property.IsStatic ())
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " is not a public instance property");
                if (property.GetGetMethod () == null || property.GetSetMethod () == null)
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " does not have both a getter and a setter");
                if (!IsAValidType (GetStructFieldType (property)))
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " has type " + property.PropertyType + ", which is not a valid kRPC type");
                if (HoldsAStream (GetStructFieldType (property)))
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " holds a stream or an event, which a struct field cannot");
                var propertyAttribute = Reflection.GetAttribute<KRPCPropertyAttribute> (property);
                if (propertyAttribute.GameScene != GameScene.Inherit)
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " sets a game scene, which struct fields cannot do");
                if (propertyAttribute.GetterId != 0 || propertyAttribute.SetterId != 0)
                    throw new ServiceException ("KRPCStruct " + type + " field " + property.Name + " sets an id, which struct fields cannot do");
            }
            if (GetStructFields (type).Count == 0)
                throw new ServiceException ("KRPCStruct " + type + " does not have any fields");
            CheckStructIsNotRecursive (type, type, new List<Type> ());
        }

        /// <summary>
        /// Check that none of the fields of the given structure type contain the structure type
        /// the check started from, whether directly or through the fields of another structure.
        /// A structure value is serialized inline, so a structure containing itself could not be
        /// serialized at all.
        /// </summary>
        static void CheckStructIsNotRecursive (Type start, Type type, IList<Type> path)
        {
            path.Add (type);
            foreach (var property in GetStructFields (type)) {
                foreach (var fieldStructType in StructTypesIn (property.PropertyType)) {
                    if (fieldStructType == start)
                        throw new ServiceException ("KRPCStruct " + start + " contains itself, via " +
                            string.Join (" -> ", path.Select (x => x.Name).Concat (new [] { start.Name }).ToArray ()));
                    if (!path.Contains (fieldStructType))
                        CheckStructIsNotRecursive (start, fieldStructType, path);
                }
            }
            path.RemoveAt (path.Count - 1);
        }

        /// <summary>
        /// The structure types that the given type is, or holds in a collection.
        /// </summary>
        static IEnumerable<Type> StructTypesIn (Type type)
        {
            type = System.Nullable.GetUnderlyingType (type) ?? type;
            if (IsAStructType (type)) {
                yield return type;
            } else if (IsACollectionType (type)) {
                foreach (var argument in type.GetGenericArguments ()) {
                    foreach (var structType in StructTypesIn (argument))
                        yield return structType;
                }
            }
        }

        /// <summary>
        /// Check the given method is a valid kRPC class method
        /// 1. Must have KRPCMethod attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public method
        /// 4. Must be declared inside a class that is assignable from the given kRPC class
        /// </summary>
        public static void ValidateKRPCMethod (Type cls, MethodBase method)
        {
            if (!Reflection.HasAttribute<KRPCMethodAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCMethod attribute");
            // Note: Type must already be a method, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (method.Name);
            // Check it's public non-static
            if (!method.IsPublic)
                throw new ServiceException ("KRPCMethod " + method + " is not public");
            // Check the method is defined in a KRPCClass
            ValidateKRPCClass (cls);
            var declaringType = method.DeclaringType;
            if (declaringType == null || !declaringType.IsAssignableFrom (cls))
                throw new ServiceException ("KRPCMethod " + method + " is not declared inside a KRPCClass");
        }

        /// <summary>
        /// Check the given type is a valid kRPC class property
        /// 1. Must have KRPCProperty attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public non-static property
        /// 4. Must be declared inside a class that is assignable from the given kRPC class
        /// </summary>
        public static void ValidateKRPCClassProperty (Type cls, PropertyInfo property)
        {
            if (!Reflection.HasAttribute<KRPCPropertyAttribute> (property))
                throw new ArgumentException (property + " does not have KRPCProperty attribute");
            // Note: Type must already be a property, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (property.Name);
            // Check it's not static
            if (!(property.IsPublic () && !property.IsStatic ()))
                throw new ServiceException ("KRPCProperty " + property + " is not public non-static");
            // Check the method is defined in a KRPCClass
            ValidateKRPCClass (cls);
            var declaringType = property.DeclaringType;
            if (declaringType == null || !declaringType.IsAssignableFrom (cls))
                throw new ServiceException ("KRPCProperty " + property + " is not declared inside a KRPCClass");
            ValidatePropertyIds (property, property.GetGetMethod () != null, property.GetSetMethod () != null);
        }

        /// <summary>
        /// Returns true if the given method is a C# extension method.
        /// </summary>
        public static bool IsAnExtensionMethod (MethodBase method)
        {
            return Reflection.HasAttribute<ExtensionAttribute> (method);
        }

        /// <summary>
        /// Returns true if the given extension property accessor is a setter.
        /// </summary>
        public static bool IsAnExtensionPropertySetter (MethodInfo method)
        {
            return method.Name.StartsWith ("Set", StringComparison.Ordinal);
        }

        /// <summary>
        /// Get the name of the property that the given accessor implements, i.e. its name
        /// without the Get or Set prefix.
        /// </summary>
        public static string GetExtensionPropertyName (MethodInfo method)
        {
            return method.Name.Substring (3);
        }

        /// <summary>
        /// Get the kRPC class that the given extension member adds a member to, which is the
        /// type of its instance parameter.
        /// </summary>
        public static Type GetExtensionTargetClass (MethodInfo method)
        {
            ValidateExtensionTarget (method);
            return method.GetParameters () [0].ParameterType;
        }

        /// <summary>
        /// Check the given method can add a member to a kRPC class
        /// 1. Must be a public static extension method
        /// 2. The type it extends must be a kRPC class
        /// </summary>
        static void ValidateExtensionTarget (MethodInfo method)
        {
            var parameters = method.GetParameters ();
            if (!(method.IsPublic && method.IsStatic && IsAnExtensionMethod (method) && parameters.Length > 0))
                throw new ServiceException ("Extension member " + method + " is not a public static extension method");
            var target = parameters [0].ParameterType;
            if (IsAStructType (target))
                throw new ServiceException ("Extension member " + method + " extends the KRPCStruct " + target + "; only kRPC classes can be extended");
            if (!IsAClassType (target))
                throw new ServiceException ("Extension member " + method + " does not extend a KRPCClass");
            ValidateKRPCClass (target);
        }

        /// <summary>
        /// Check the given method is a valid kRPC extension method
        /// 1. Must have KRPCMethod attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public static extension method of a kRPC class
        /// </summary>
        public static void ValidateKRPCExtensionMethod (MethodInfo method)
        {
            if (!Reflection.HasAttribute<KRPCMethodAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCMethod attribute");
            ValidateIdentifier (method.Name);
            ValidateExtensionTarget (method);
        }

        /// <summary>
        /// Check the given method is a valid kRPC extension property accessor
        /// 1. Must have KRPCProperty attribute
        /// 2. Must be named GetX or SetX, where X is a valid identifier
        /// 3. Must be a public static extension method of a kRPC class
        /// 4. A getter must take only the instance and return a value
        /// 5. A setter must take the instance and the value to set, and return nothing
        /// </summary>
        public static void ValidateKRPCExtensionProperty (MethodInfo method)
        {
            if (!Reflection.HasAttribute<KRPCPropertyAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCProperty attribute");
            var isSetter = IsAnExtensionPropertySetter (method);
            if (!isSetter && !method.Name.StartsWith ("Get", StringComparison.Ordinal))
                throw new ServiceException ("KRPCProperty " + method + " is not named GetX or SetX");
            ValidateIdentifier (GetExtensionPropertyName (method));
            ValidateExtensionTarget (method);
            var numParameters = method.GetParameters ().Length;
            if (isSetter && !(numParameters == 2 && method.ReturnType == typeof(void)))
                throw new ServiceException ("KRPCProperty " + method + " is not a property setter; it must take the value to set and return nothing");
            if (!isSetter && !(numParameters == 1 && method.ReturnType != typeof(void)))
                throw new ServiceException ("KRPCProperty " + method + " is not a property getter; it must take no arguments and return a value");
            ValidatePropertyIds (method, !isSetter, isSetter);
        }

        /// <summary>
        /// Check the given type is a valid kRPC exception class
        /// 1. Must have KRPCException attribute
        /// 2. Must have a valid identifier
        /// 3. Must be a public non-static class
        /// 4. Must be declared inside a kRPC service if it doesn't have the service explicity set
        /// 5. Must not be declared inside a kRPC service if it does have the service explicity set
        /// </summary>
        public static void ValidateKRPCException (Type type)
        {
            if (!Reflection.HasAttribute<KRPCExceptionAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCException attribute");
            var attribute = Reflection.GetAttribute<KRPCExceptionAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (type.Name);
            // Check it's public non-static
            if (!((type.IsPublic || type.IsNestedPublic) && !type.IsStatic ()))
                throw new ServiceException ("KRPCException " + type + " is not public non-static");
            // If it doesn't have the Service property set, check the class is defined directly inside a KRPCService
            var declaringType = type.DeclaringType;
            if (attribute.Service == null && (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType)))
                throw new ServiceException ("KRPCException " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the class isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCException " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
        }

        /// <summary>
        /// Returns whether the procedure/method can return null.
        /// </summary>
        public static bool GetNullable (ICustomAttributeProvider member)
        {
            if (Reflection.HasAttribute<KRPCProcedureAttribute> (member))
                return Reflection.GetAttribute<KRPCProcedureAttribute> (member).Nullable;
            if (Reflection.HasAttribute<KRPCMethodAttribute> (member))
                return Reflection.GetAttribute<KRPCMethodAttribute> (member).Nullable;
            if (Reflection.HasAttribute<KRPCPropertyAttribute> (member))
                return Reflection.GetAttribute<KRPCPropertyAttribute> (member).Nullable;
            throw new ArgumentException ("member is not a kRPC procedure, attribute or property", nameof(member));
        }

        /// <summary>
        /// The kRPC type of a structure field. A Nullable&lt;T&gt; field is represented by its
        /// underlying type T.
        /// </summary>
        static Type GetStructFieldType (PropertyInfo field)
        {
            return System.Nullable.GetUnderlyingType (field.PropertyType) ?? field.PropertyType;
        }

        /// <summary>
        /// Whether a structure field can be null, which it is when its type is Nullable&lt;T&gt;
        /// or it is marked with KRPCProperty Nullable.
        /// </summary>
        static bool GetStructFieldNullable (PropertyInfo field)
        {
            return System.Nullable.GetUnderlyingType (field.PropertyType) != null ||
                Reflection.GetAttribute<KRPCPropertyAttribute> (field).Nullable;
        }

        /// <summary>
        /// The spec for a structure field, naming the field and the structure it is in when a
        /// nullable position cannot be reached.
        /// </summary>
        public static TypeSpec GetStructFieldSpec (Type type, PropertyInfo field)
        {
            return TypeSpec.Create (
                field.PropertyType, GetStructFieldNullable (field), GetNullablePaths (field),
                type.Name + " field " + field.Name);
        }

        /// <summary>
        /// The fields of the given structure type and their specs, in the order their values
        /// are encoded in. Built once per structure, as encoding a value reads both for every
        /// value.
        /// </summary>
        public static void GetStructFieldsAndSpecs (
            Type type, out IList<PropertyInfo> fields, out IList<TypeSpec> specs)
        {
            lock (structFieldSpecs) {
                KeyValuePair<IList<PropertyInfo>, IList<TypeSpec>> entry;
                if (!structFieldSpecs.TryGetValue (type, out entry)) {
                    var typeFields = GetStructFields (type);
                    entry = new KeyValuePair<IList<PropertyInfo>, IList<TypeSpec>> (
                        typeFields,
                        typeFields.Select (field => GetStructFieldSpec (type, field)).ToList ());
                    structFieldSpecs [type] = entry;
                }
                fields = entry.Key;
                specs = entry.Value;
            }
        }

        /// <summary>
        /// The paths the KRPCNullable attributes on the given member name, each a nullable
        /// position inside its type.
        /// </summary>
        public static IEnumerable<IList<Position>> GetNullablePaths (ICustomAttributeProvider member)
        {
            return Reflection.GetAttributes<KRPCNullableAttribute> (member)
                .Select (attribute => attribute.Path);
        }

        /// <summary>
        /// Serialize a type into a dictionary for use in a service definition, from the spec
        /// that carries the nullability of every position inside it.
        /// </summary>
        public static object SerializeType (TypeSpec spec)
        {
            var type = spec.Type;
            var nullable = spec.Nullable;
            if (!IsAValidType (type))
                throw new ArgumentException ("Type " + type + " is not a valid kRPC type");
            var result = new OrderedDictionary ();
            if (IsAValueType (type)) {
                switch (Type.GetTypeCode (type)) {
                case TypeCode.Double:
                    result["code"] = "DOUBLE";
                    break;
                case TypeCode.Single:
                    result["code"] = "FLOAT";
                    break;
                case TypeCode.Int32:
                    result["code"] = "SINT32";
                    break;
                case TypeCode.Int64:
                    result["code"] = "SINT64";
                    break;
                case TypeCode.UInt32:
                    result["code"] = "UINT32";
                    break;
                case TypeCode.UInt64:
                    result["code"] = "UINT64";
                    break;
                case TypeCode.Boolean:
                    result["code"] = "BOOL";
                    break;
                case TypeCode.String:
                    result["code"] = "STRING";
                    break;
                default:
                    if (type == typeof(byte[]))
                        result["code"] = "BYTES";
                    break;
                }
            } else if (IsAClassType (type)) {
                result["code"] = "CLASS";
                result["service"] = GetClassServiceName (type);
                result["name"] = type.Name;
            } else if (IsAnEnumType (type)) {
                result["code"] = "ENUMERATION";
                result["service"] = GetEnumServiceName (type);
                result["name"] = type.Name;
            } else if (IsAStructType (type)) {
                result["code"] = "STRUCT";
                result["service"] = GetStructServiceName (type);
                result["name"] = type.Name;
            } else if (IsATupleCollectionType (type)) {
                result["code"] = "TUPLE";
                result["types"] = spec.Types.Select (SerializeType).ToList ();
            } else if (IsAListCollectionType (type)) {
                result["code"] = "LIST";
                result["types"] = spec.Types.Select (SerializeType).ToList ();
            } else if (IsASetCollectionType (type)) {
                result["code"] = "SET";
                result["types"] = spec.Types.Select (SerializeType).ToList ();
            } else if (IsADictionaryCollectionType (type)) {
                result["code"] = "DICTIONARY";
                result["types"] = spec.Types.Select (SerializeType).ToList ();
            } else if (IsAMessageType (type)) {
                result["code"] = Naming.UpperSnakeCase (type.Name);
            }
            if (!result.Contains ("code"))
                throw new ArgumentException ("Type " + type + " is not a valid kRPC type");
            if (nullable)
                result["nullable"] = true;
            return result;
        }
    }
}
