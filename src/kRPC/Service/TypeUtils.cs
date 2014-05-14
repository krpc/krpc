using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using KRPC.Utils;
using KRPC.Service.Attributes;
using KRPC.Schema;

namespace KRPC.Service
{
    static class TypeUtils
    {
        /// <summary>
        /// Returns true if the given identifier is a valid kRPC identifier.
        /// A valid identifier is a non-zero length string, containing letters and numbers,
        /// and starting with a letter.
        /// </summary>
        public static bool IsAValidIdentifier (string identifier)
        {
            var regex = new Regex ("^[a-z][a-z0-9]*$", RegexOptions.IgnoreCase);
            return regex.IsMatch (identifier);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC type
        /// </summary>
        public static bool IsAValidType (Type type)
        {
            return ProtocolBuffers.IsAValidType (type) || IsAClassType (type) || IsAnEnumType (type) || IsACollectionType (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC key type
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
        /// Returns true if the given type can be used as a kRPC class type
        /// </summary>
        public static bool IsAClassType (Type type)
        {
            return Reflection.HasAttribute<KRPCClassAttribute> (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC enum type
        /// </summary>
        public static bool IsAnEnumType (Type type)
        {
            return Reflection.HasAttribute<KRPCEnumAttribute> (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC collection type
        /// </summary>
        public static bool IsACollectionType (Type type)
        {
            return IsAListCollectionType (type) || IsADictionaryCollectionType (type) || IsASetCollectionType (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC list collection type
        /// </summary>
        public static bool IsAListCollectionType (Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IList<>) &&
                   IsAValidType (type.GetGenericArguments ().Single ());
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC dictionary collection type
        /// </summary>
        public static bool IsADictionaryCollectionType (Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>) &&
                   IsAValidKeyType (type.GetGenericArguments ()[0]) &&
                   IsAValidType (type.GetGenericArguments ()[1]);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC list collection type
        /// </summary>
        public static bool IsASetCollectionType (Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition () == typeof (HashSet<>) &&
                   IsAValidType (type.GetGenericArguments ().Single ());
        }

        /// <summary>
        /// Return the name of the protocol buffer type for the given C# type
        /// </summary>
        public static string GetTypeName (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ("Type is not valid");
            else if (ProtocolBuffers.IsAValidType (type))
                return ProtocolBuffers.GetTypeName (type);
            else if (IsAClassType (type))
                return ProtocolBuffers.GetTypeName (typeof(ulong)); // Class instance GUIDs are uint64
            else if (IsAnEnumType (type))
                return ProtocolBuffers.GetTypeName (typeof(int)); // Enums are int32
            else if (IsAListCollectionType (type))
                return ProtocolBuffers.GetMessageTypeName (typeof (global::KRPC.Schema.KRPC.List));
            else if (IsADictionaryCollectionType (type))
                return ProtocolBuffers.GetMessageTypeName (typeof (global::KRPC.Schema.KRPC.Dictionary));
            else if (IsASetCollectionType (type))
                return ProtocolBuffers.GetMessageTypeName (typeof (global::KRPC.Schema.KRPC.Set));
            else
                throw new ArgumentException ("Type is not valid");
        }

        /// <summary>
        /// Return the name of the kRPC type for the given C# type
        /// </summary>
        public static string GetKRPCTypeName (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type))
                return "Class(" + GetClassServiceName (type) + "." + type.Name + ")";
            else if (IsAnEnumType (type))
                return "Enum(" + GetEnumServiceName (type) + "." + type.Name + ")";
            else if (IsAListCollectionType (type))
                return "List(" + GetKRPCTypeName (type.GetGenericArguments ().Single ()) + ")";
            else if (IsADictionaryCollectionType (type))
                return "Dictionary(" + GetKRPCTypeName (type.GetGenericArguments ()[0]) + "," +
                                       GetKRPCTypeName (type.GetGenericArguments ()[1]) + ")";
            else if (IsASetCollectionType (type))
                return "Set(" + GetKRPCTypeName (type.GetGenericArguments ().Single ()) + ")";
            else
                return ProtocolBuffers.GetTypeName (type);
        }

        /// <summary>
        /// Get the parameter type attributes for the given kRPC procedure parameter
        /// </summary>
        public static string[] ParameterTypeAttributes (int position, Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type) || IsAnEnumType (type) || IsACollectionType (type))
                return new [] { "ParameterType(" + position + ")." + GetKRPCTypeName (type) };
            else
                return new string[] { };
        }

        /// <summary>
        /// Get the return type attributes for the given type
        /// </summary>
        public static string[] ReturnTypeAttributes (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type) || IsAnEnumType (type) || IsACollectionType (type))
                return new [] { "ReturnType." + GetKRPCTypeName (type) };
            else
                return new string[] { };
        }

        /// <summary>
        /// Get the name of the service for the given KRPCService annotated type
        /// </summary>
        public static string GetServiceName (Type type)
        {
            ValidateKRPCService (type);
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            var name = attribute.Name;
            if (name != null)
                return name;
            return type.Name;
        }

        /// <summary>
        /// Get the name of the service for the given KRPCClass annotated type
        /// </summary>
        public static string GetClassServiceName (Type type)
        {
            ValidateKRPCClass (type);
            var attribute = Reflection.GetAttribute<KRPCClassAttribute> (type);
            return attribute.Service == null ? GetServiceName (type.DeclaringType) : attribute.Service;
        }

        /// <summary>
        /// Get the name of the service for the given KRPCEnum annotated type
        /// </summary>
        public static string GetEnumServiceName (Type type)
        {
            ValidateKRPCEnum (type);
            var attribute = Reflection.GetAttribute<KRPCEnumAttribute> (type);
            return attribute.Service == null ? GetServiceName (type.DeclaringType) : attribute.Service;
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
        ///  1. Must have KRPCService attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public static class
        ///  4. Must not be declared inside another kRPC service
        /// </summary>
        public static void ValidateKRPCService (Type type)
        {
            if (!Reflection.HasAttribute<KRPCServiceAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCService attribute");
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier. If Name is specified, use that as the identifier.
            ValidateIdentifier (attribute.Name == null ? type.Name : attribute.Name);
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
        ///  1. Must have KRPCProcedure attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public static method
        ///  4. Must be declared inside a kRPC service
        /// </summary>
        public static void ValidateKRPCProcedure (MethodInfo method)
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
        ///  1. Must have KRPCProperty attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public static property
        ///  4. Must be declared inside a kRPC service
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
        ///  1. Must have KRPCClass attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public non-static class
        ///  4. Must be declared inside a kRPC service if it doesn't have the service explicity set
        ///  5. Must not be declared inside a kRPC service if it does have the service explicity set
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
            if (attribute.Service == null && (type.DeclaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (type.DeclaringType)))
                throw new ServiceException ("KRPCClass " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the class isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                var declaringType = type.DeclaringType;
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCClass " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
        }

        /// <summary>
        /// Check the given type is a valid kRPC enumeration
        ///  1. Must have KRPCEnum attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public enum
        ///  4. Underlying type must be a 32-bit signed integer (int)
        ///  5. Must be declared inside a kRPC service if it doesn't have the service explicity set
        ///  6. Must not be declared inside a kRPC service if it does have the service explicity set
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
            if (Enum.GetUnderlyingType (type) != typeof (int))
                throw new ServiceException ("KRPCEnum " + type + " has underlying type " + Enum.GetUnderlyingType (type) + "; but only int is supported");
            // If it doesn't have the Service property set, check the enum is defined directly inside a KRPCService
            if (attribute.Service == null && (type.DeclaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (type.DeclaringType)))
                throw new ServiceException ("KRPCEnum " + type + " is not declared inside a KRPCService");
            // If it does have the Service property set, check the class isn't defined in a KRPCService
            if (attribute.Service != null) {
                ValidateIdentifier (attribute.Service);
                var declaringType = type.DeclaringType;
                while (declaringType != null) {
                    if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                        throw new ServiceException ("KRPCClass " + type + " is declared inside a KRPCService, but has the service name explicitly set");
                    declaringType = declaringType.DeclaringType;
                }
            }
        }

        /// <summary>
        /// Check the given method is a valid kRPC class method
        ///  1. Must have KRPCMethod attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public non-static method
        ///  4. Must be declared inside a kRPC class
        /// </summary>
        public static void ValidateKRPCMethod (MethodInfo method)
        {
            if (!Reflection.HasAttribute<KRPCMethodAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCMethod attribute");
            // Note: Type must already be a method, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (method.Name);
            // Check it's public non-static
            if (!(method.IsPublic && !method.IsStatic))
                throw new ServiceException ("KRPCMethod " + method + " is not public non-static");
            // Check the class is defined in a KRPCClass
            var declaringType = method.DeclaringType;
            if (!Reflection.HasAttribute<KRPCClassAttribute> (declaringType))
                throw new ServiceException ("KRPCMethod " + method + " is not declared inside a KRPCClass");
        }

        /// <summary>
        /// Check the given type is a valid kRPC class property
        ///  1. Must have KRPCProperty attribute
        ///  2. Must have a valid identifier
        ///  3. Must be a public non-static property
        ///  4. Must be declared inside a kRPC class
        /// </summary>
        public static void ValidateKRPCClassProperty (PropertyInfo property)
        {
            if (!Reflection.HasAttribute<KRPCPropertyAttribute> (property))
                throw new ArgumentException (property + " does not have KRPCProperty attribute");
            // Note: Type must already be a property, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (property.Name);
            // Check it's not static
            if (!(property.IsPublic () && !property.IsStatic ()))
                throw new ServiceException ("KRPCProperty " + property + " is not public non-static");
            // Check the method is defined directly inside a KRPCClass
            var declaringType = property.DeclaringType;
            if (declaringType == null || !Reflection.HasAttribute<KRPCClassAttribute> (declaringType))
                throw new ServiceException ("KRPCProperty " + property + " is not declared inside a KRPCClass");
        }
    }
}

