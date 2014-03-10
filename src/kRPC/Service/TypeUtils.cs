using System;
using KRPC.Utils;
using KRPC.Service.Attributes;
using System.Reflection;
using System.Text.RegularExpressions;

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
            return ProtocolBuffers.IsAValidType (type) || IsAClassType (type);
        }

        /// <summary>
        /// Returns true if the given type can be used as a kRPC class type
        /// </summary>
        public static bool IsAClassType (Type type)
        {
            return Reflection.HasAttribute<KRPCClassAttribute> (type);
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
            else
                return ProtocolBuffers.GetTypeName (typeof(ulong)); // Class instance GUIDs are uint64
        }

        /// <summary>
        /// Get the parameter type attributes for the given kRPC procedure parameter
        /// </summary>
        public static string[] ParameterTypeAttributes (int position, Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type))
                return new [] { "ParameterType(" + position + ").Class(" + Scanner.Utils.GetServiceFor (type) + "." + type.Name + ")" };
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
            else if (IsAClassType (type))
                return new [] { "ReturnType.Class(" + Scanner.Utils.GetServiceFor (type) + "." + type.Name + ")" };
            else
                return new string[] { };
        }

        /// <summary>
        /// Get the name of the service for the given KRPCService annotated type
        /// </summary>
        public static string GetServiceName (Type type)
        {
            if (!Reflection.HasAttribute<KRPCServiceAttribute> (type))
                throw new ArgumentException ();
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            var name = attribute.Name;
            if (name != null)
                return name;
            return type.Name;
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
        /// </summary>
        public static void ValidateKRPCService (Type type)
        {
            if (!Reflection.HasAttribute<KRPCServiceAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCService attribute");
            var attribute = Reflection.GetAttribute<KRPCServiceAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier. If Name is specified, use that as the identifier.
            ValidateIdentifier (attribute.Name == null ? type.Name : attribute.Name);
            // Check it's static
            if (!type.IsStatic ())
                throw new ServiceException ("KRPCService " + type + " must be static");
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
        /// </summary>
        public static void ValidateKRPCProcedure (MethodInfo method)
        {
            if (!Reflection.HasAttribute<KRPCProcedureAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCProcedure attribute");
            // Note: Type must already be a method, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (method.Name);
            // Check the method is static
            if (!method.IsStatic)
                throw new ServiceException ("KRPCService " + method + " is not static");
            // Check its defined directly in a KRPCServiceAttribute
            var declaringType = method.DeclaringType;
            if (declaringType == null || !Reflection.HasAttribute<KRPCServiceAttribute> (declaringType))
                throw new ServiceException ("KRPCProcedure " + method + " is not declared inside a KRPCService");
        }

        /// <summary>
        /// Check the given type is a valid kRPC property
        /// </summary>
        public static void ValidateKRPCProperty (PropertyInfo property)
        {
            if (!Reflection.HasAttribute<KRPCPropertyAttribute> (property))
                throw new ArgumentException (property + " does not have KRPCProperty attribute");
            // Note: Type must already be a property, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (property.Name);
            // Check the method is defined directly inside a KRPCService or KRPCClass
            var declaringType = property.DeclaringType;
            if (declaringType == null ||
                (!Reflection.HasAttribute<KRPCServiceAttribute> (declaringType) && !Reflection.HasAttribute<KRPCClassAttribute> (declaringType))) {
                throw new ServiceException ("KRPCProperty " + property + " is not declared inside a KRPCService or KRPCClass");
            }
            if (Reflection.HasAttribute<KRPCServiceAttribute> (declaringType)) {
                // If inside a KRPCService, check it's static
                if (!property.IsStatic ())
                    throw new ServiceException ("KRPCProperty " + property + " must be static");
            } else {
                // If inside a KRPCClass, check it's not static
                if (property.IsStatic ())
                    throw new ServiceException ("KRPCProperty " + property + " must not be static");
            }
        }

        /// <summary>
        /// Check the given type is a valid kRPC class
        /// </summary>
        public static void ValidateKRPCClass (Type type)
        {
            if (!Reflection.HasAttribute<KRPCClassAttribute> (type))
                throw new ArgumentException (type + " does not have KRPCClass attribute");
            var attribute = Reflection.GetAttribute<KRPCClassAttribute> (type);
            // Note: Type must already be a class, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (type.Name);
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
            // Check the class is not static
            if (type.IsStatic ())
                throw new ServiceException ("KRPCClass " + type + " must not be static");
        }

        /// <summary>
        /// Check the given method is a valid kRPC method
        /// </summary>
        public static void ValidateKRPCMethod (MethodInfo method)
        {
            if (!Reflection.HasAttribute<KRPCMethodAttribute> (method))
                throw new ArgumentException (method + " does not have KRPCMethod attribute");
            // Note: Type must already be a method, due to AttributeUsage definition
            // Validate the identifier.
            ValidateIdentifier (method.Name);
            // Check the method is not static
            if (method.IsStatic)
                throw new ServiceException ("KRPCMethod " + method + " must not be static");
        }
    }
}

