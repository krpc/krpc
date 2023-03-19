using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        static Regex identifierPattern = new Regex ("^[A-Z][A-Za-z0-9]*$");

        /// <summary>
        /// Returns true if the given identifier is a valid kRPC identifier.
        /// A valid identifier is a non-zero length string, containing letters and numbers,
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
            return IsAValueType (type) || IsAMessageType (type) || IsAClassType (type) || IsAnEnumType (type) || IsACollectionType (type);
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
        /// Returns true if the given type can be used as a kRPC collection type.
        /// </summary>
        public static bool IsACollectionType (Type type)
        {
            return IsATupleCollectionType (type) || IsAListCollectionType (type) || IsASetCollectionType (type) || IsADictionaryCollectionType (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC tuple collection type.
        /// </summary>
        public static bool IsATupleCollectionType (Type type)
        {
            return type.Name.StartsWith("Tuple`", StringComparison.CurrentCulture) &&
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
            var id = attribute.Id;
            if (id == 0) {
                // Generate a service id from the service name
                var serviceName = TypeUtils.GetServiceName(type);
                using (var sha256 = SHA256.Create()) {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(serviceName));
                    id= (uint)BitConverter.ToInt64(hash, 0);
                }
            }
            return id;
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
        /// Get the name of the service for the given KRPCEnum annotated type
        /// </summary>
        public static string GetEnumServiceName (Type type)
        {
            ValidateKRPCEnum (type);
            var attribute = Reflection.GetAttribute<KRPCEnumAttribute> (type);
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
        /// Serialize a type into a dictionary for use in a service definition.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static object SerializeType (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ("Type " + type + " is not a valid kRPC type");
            var result = new Dictionary<string,object> ();
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
            } else if (IsATupleCollectionType (type)) {
                result["code"] = "TUPLE";
                result["types"] = type.GetGenericArguments ().Select (t => SerializeType (t)).ToList ();
            } else if (IsAListCollectionType (type)) {
                result["code"] = "LIST";
                result["types"] = type.GetGenericArguments ().Select (t => SerializeType (t)).ToList ();
            } else if (IsASetCollectionType (type)) {
                result["code"] = "SET";
                result["types"] = type.GetGenericArguments ().Select (t => SerializeType (t)).ToList ();
            } else if (IsADictionaryCollectionType (type)) {
                result["code"] = "DICTIONARY";
                result["types"] = type.GetGenericArguments ().Select (t => SerializeType (t)).ToList ();
            } else if (IsAMessageType (type)) {
                var name = type.ToString ();
                var camelCase = name.Substring (name.LastIndexOf ('.') + 1);
                var snakeCase = string.Empty;
                for (var i = 0; i < camelCase.Length-1; i++) {
                    if (char.IsLower(camelCase[i]) && char.IsUpper(camelCase[i+1]))
                        snakeCase += camelCase[i] + "_";
                    else
                        snakeCase += camelCase[i];
                }
                snakeCase += camelCase[camelCase.Length-1];
                result["code"] = snakeCase.ToUpper();
            }
            if (!result.ContainsKey("code"))
                throw new ArgumentException ("Type " + type + " is not a valid kRPC type");
            return result;
        }
    }
}
