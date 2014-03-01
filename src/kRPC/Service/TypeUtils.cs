using System;
using KRPC.Utils;
using KRPC.Service.Attributes;

namespace KRPC.Service
{
    static class TypeUtils
    {
        public static bool IsAValidType (Type type)
        {
            return ProtocolBuffers.IsAValidType (type) || IsAClassType (type);
        }

        public static bool IsAClassType (Type type)
        {
            return type.IsDefined (typeof(KRPCClass), false);
        }

        public static string GetTypeName (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ("Type is not valid");
            else if (ProtocolBuffers.IsAValidType (type))
                return ProtocolBuffers.GetTypeName (type);
            else
                return ProtocolBuffers.GetTypeName (typeof(ulong)); // Class instance GUIDs are uint64
        }

        public static string[] ParameterTypeAttributes (int position, Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type))
                return new [] { "ParameterType(" + position + ").Class(" + Scanner.Utils.GetServiceFor (type) + "." + type.Name + ")" };
            else
                return new string[] { };
        }

        public static string[] ReturnTypeAttributes (Type type)
        {
            if (!IsAValidType (type))
                throw new ArgumentException ();
            else if (IsAClassType (type))
                return new [] { "ReturnType.Class(" + Scanner.Utils.GetServiceFor (type) + "." + type.Name + ")" };
            else
                return new string[] { };
        }
    }
}

