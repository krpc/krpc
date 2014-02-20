using System;
using System.Reflection;
using Google.ProtocolBuffers;

namespace KRPC.Utils
{
    class ProtocolBuffers
    {
        /// <summary>
        /// Return the string name of the Protocol Buffer message type (with the package name prefixing it).
        /// E.g. "KRPC.Request"
        /// </summary>
        public static string GetMessageTypeName (Type type) {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer message type");
            return type.FullName.Replace("KRPC.Schema.", "");
        }

        /// <summary>
        /// Return the string name of the Protocol Buffer message type corresponding to the given type.
        /// E.g. "uint32" for uint
        /// </summary>
        public static string GetValueTypeName (Type type)
        {
            // Note: C# has no equivalent types for sint32, sint64, fixed32, fixed64, sfixed32 or sfixed64
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer value type");
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(int))
                return "int32";
            else if (type == typeof(long))
                return "int64";
            else if (type == typeof(uint))
                return "uint32";
            else if (type == typeof(ulong))
                return "uint64";
            else if (type == typeof(bool))
                return "bool";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(byte[]))
                return "bytes";
            else
                throw new ArgumentException (type.ToString() + " is not a Protocol Buffer value type");
        }

        /// <summary>
        /// Return a builder object for the given Protocol Buffer message type.
        /// The type must be derived from IMessage.
        /// </summary>
        public static IBuilder BuilderForMessageType (Type type)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer message type");
            MethodInfo createBuilder = type.GetMethod ("CreateBuilder", new Type[] {});
            return (IBuilder) createBuilder.Invoke (null, null);
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            return typeof(IMessage).IsAssignableFrom (type);
        }

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
    }
}

