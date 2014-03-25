using System;
using System.IO;
using System.Reflection;
using Google.ProtocolBuffers;

namespace KRPC.Utils
{
    static class ProtocolBuffers
    {
        /// <summary>
        /// Return the string name of the Protocol Buffer message type (with the package name prefixing it).
        /// E.g. "KRPC.Request"
        /// </summary>
        public static string GetMessageTypeName (Type type)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type + " is not a Protocol Buffer message type");
            return type.FullName.Replace ("KRPC.Schema.", "");
        }

        /// <summary>
        /// Return the string name of the Protocol Buffer enumeration type (with the package name prefixing it).
        /// </summary>
        public static string GetEnumTypeName (Type type)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer enumeration type");
            if (!IsAnEnumType (type))
                throw new ArgumentException (type + " is not a Protocol Buffer enumeration type");
            return type.FullName.Replace ("KRPC.Schema.", "");
        }

        /// <summary>
        /// Return the string name of the Protocol Buffer message type corresponding to the given type.
        /// E.g. "uint32" for uint
        /// </summary>
        public static string GetValueTypeName (Type type)
        {
            // Note: C# has no equivalent types for sint32, sint64, fixed32, fixed64, sfixed32 or sfixed64
            if (type == typeof(double))
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
            else {
                if (type == null)
                    throw new ArgumentException ("null is not a Protocol Buffer value type");
                else
                    throw new ArgumentException (type.ToString () + " is not a Protocol Buffer value type");
            }
        }

        /// <summary>
        /// Return the string name of the Protocol Buffer message, enumeration or value type.
        /// </summary>
        public static string GetTypeName (Type type)
        {
            if (IsAMessageType (type))
                return GetMessageTypeName (type);
            else if (IsAnEnumType (type))
                return GetEnumTypeName (type);
            else if (IsAValueType (type))
                return GetValueTypeName (type);
            else
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer message, enumeration or value type");
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
                throw new ArgumentException (type + " is not a Protocol Buffer message type");
            MethodInfo createBuilder = type.GetMethod ("CreateBuilder", new Type[] { });
            return (IBuilder)createBuilder.Invoke (null, null);
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            return typeof(IMessage).IsAssignableFrom (type);
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer enumeration type.
        /// </summary>
        public static bool IsAnEnumType (Type type)
        {
            // TODO: is this sufficient?
            if (type == null)
               return false;
            return type.FullName.StartsWith("KRPC.Schema.") ? type.IsEnum : false;
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer value type.
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
        /// Returns true if the given type is a Protocol Buffer message or value type.
        /// </summary>
        public static bool IsAValidType (Type type)
        {
            return IsAValueType (type) || IsAMessageType (type) || IsAnEnumType (type);
        }

        /// <summary>
        /// Convert a Protocol Buffer message to a byte string.
        /// </summary>
        public static ByteString WriteMessage (IMessage message)
        {
            byte[] returnBytes;
            using (var stream = new MemoryStream ()) {
                message.WriteTo (stream);
                returnBytes = stream.ToArray ();
            }
            return ByteString.CopyFrom (returnBytes);
        }

        /// <summary>
        /// Convert a Protocol Buffer value type, encoded as a byte string, to a C# value.
        /// </summary>
        public static object ReadValue (ByteString value, Type type)
        {
            var stream = CodedInputStream.CreateInstance (value.ToByteArray ());
            if (type == typeof(double)) {
                double result = 0;
                stream.ReadDouble (ref result);
                return result;
            } else if (type == typeof(float)) {
                float result = 0;
                stream.ReadFloat (ref result);
                return result;
            } else if (type == typeof(int)) {
                int result = 0;
                stream.ReadInt32 (ref result);
                return result;
            } else if (type == typeof(long)) {
                long result = 0;
                stream.ReadInt64 (ref result);
                return result;
            } else if (type == typeof(uint)) {
                uint result = 0;
                stream.ReadUInt32 (ref result);
                return result;
            } else if (type == typeof(ulong)) {
                ulong result = 0;
                stream.ReadUInt64 (ref result);
                return result;
            } else if (type == typeof(bool)) {
                bool result = false;
                stream.ReadBool (ref result);
                return result;
            } else if (type == typeof(string)) {
                string result = "";
                stream.ReadString (ref result);
                return result;
            } else if (type == typeof(byte[])) {
                ByteString result = null;
                stream.ReadBytes (ref result);
                return result.ToByteArray ();
            }
            throw new ArgumentException (type.ToString () + " is not a Protocol Buffer value type");
        }

        /// <summary>
        /// Convert a Protocol Buffer value type from a C# value to a byte string.
        /// </summary>
        public static ByteString WriteValue (object value, Type type)
        {
            var stream = new MemoryStream ();
            var encoder = CodedOutputStream.CreateInstance (stream);
            if (type == typeof(double))
                encoder.WriteDoubleNoTag ((double)value);
            else if (type == typeof(float))
                encoder.WriteFloatNoTag ((float)value);
            else if (type == typeof(int))
                encoder.WriteInt32NoTag ((int)value);
            else if (type == typeof(long))
                encoder.WriteInt64NoTag ((long)value);
            else if (type == typeof(uint))
                encoder.WriteUInt32NoTag ((uint)value);
            else if (type == typeof(ulong))
                encoder.WriteUInt64NoTag ((ulong)value);
            else if (type == typeof(bool))
                encoder.WriteBoolNoTag ((bool)value);
            else if (type == typeof(string))
                encoder.WriteStringNoTag ((string)value);
            else if (type == typeof(byte[]))
                encoder.WriteBytesNoTag (ByteString.CopyFrom ((byte[])value));
            else
                throw new ArgumentException (type.ToString () + " is not a Protocol Buffer value type");
            encoder.Flush ();
            return ByteString.CopyFrom (stream.ToArray ());
        }
    }
}

