using System;
using System.IO;
using System.Reflection;
using Google.Protobuf;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Service;

namespace KRPC.Utils
{
    static class ProtocolBuffers
    {
        public static byte[] Encode (object value, Type type)
        {
            if (TypeUtils.IsAClassType (type))
                return ProtocolBuffers.WriteValue (ObjectStore.Instance.AddInstance (value), typeof(ulong)).ToByteArray ();
            else if (TypeUtils.IsAnEnumType (type)) {
                // TODO: Assumes it's underlying type is int
                return ProtocolBuffers.WriteValue ((int)value, typeof(int)).ToByteArray ();
            } else if (ProtocolBuffers.IsAMessageType (type))
                return ProtocolBuffers.WriteMessage (value as Google.Protobuf.IMessage).ToByteArray ();
            else
                return ProtocolBuffers.WriteValue (value, type).ToByteArray ();
        }

        /// <summary>
        /// Parse the given data into a message object of the given type.
        /// The type must be derived from IMessage.
        /// </summary>
        public static Google.Protobuf.IMessage ParseFrom (Type type, ByteString value)
        {
            if (type == null)
                throw new ArgumentException ("null is not a Protocol Buffer message type");
            if (!IsAMessageType (type))
                throw new ArgumentException (type + " is not a Protocol Buffer message type");
            var stream = new CodedInputStream (value.ToByteArray ());
            var message = (Google.Protobuf.IMessage)Activator.CreateInstance (type);
            message.MergeFrom (stream);
            return message;
        }

        /// <summary>
        /// Returns true if the given type is a Protocol Buffer message type.
        /// </summary>
        public static bool IsAMessageType (Type type)
        {
            return typeof(Google.Protobuf.IMessage).IsAssignableFrom (type);
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
            return IsAValueType (type) || IsAMessageType (type);
        }

        /// <summary>
        /// Convert a Protocol Buffer message to a byte string.
        /// </summary>
        public static ByteString WriteMessage (Google.Protobuf.IMessage message)
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
            if (value.Length == 0)
                throw new ArgumentException ("Value is empty");
            var stream = new CodedInputStream (value.ToByteArray ());
            if (type == typeof(double)) {
                return stream.ReadDouble ();
            } else if (type == typeof(float)) {
                return stream.ReadFloat ();
            } else if (type == typeof(int)) {
                return stream.ReadInt32 ();
            } else if (type == typeof(long)) {
                return stream.ReadInt64 ();
            } else if (type == typeof(uint)) {
                return stream.ReadUInt32 ();
            } else if (type == typeof(ulong)) {
                return stream.ReadUInt64 ();
            } else if (type == typeof(bool)) {
                return stream.ReadBool ();
            } else if (type == typeof(string)) {
                return stream.ReadString ();
            } else if (type == typeof(byte[])) {
                return stream.ReadBytes ().ToByteArray ();
            }
            throw new ArgumentException (type + " is not a Protocol Buffer value type");
        }

        /// <summary>
        /// Convert a Protocol Buffer value type from a C# value to a byte string.
        /// </summary>
        public static ByteString WriteValue (object value, Type type)
        {
            var stream = new MemoryStream ();
            var encoder = new CodedOutputStream (stream);
            if (type == typeof(double))
                encoder.WriteDouble ((double)value);
            else if (type == typeof(float))
                encoder.WriteFloat ((float)value);
            else if (type == typeof(int))
                encoder.WriteInt32 ((int)value);
            else if (type == typeof(long))
                encoder.WriteInt64 ((long)value);
            else if (type == typeof(uint))
                encoder.WriteUInt32 ((uint)value);
            else if (type == typeof(ulong))
                encoder.WriteUInt64 ((ulong)value);
            else if (type == typeof(bool))
                encoder.WriteBool ((bool)value);
            else if (type == typeof(string))
                encoder.WriteString ((string)value);
            else if (type == typeof(byte[]))
                encoder.WriteBytes (ByteString.CopyFrom ((byte[])value));
            else
                throw new ArgumentException (type + " is not a Protocol Buffer value type");
            encoder.Flush ();
            return ByteString.CopyFrom (stream.ToArray ());
        }
    }
}

