using System;
using System.IO;
using Google.Protobuf;
using KRPC.Service;
using System.Collections;
using System.Linq;
using System.Reflection;
using KRPC.Service.Scanner;

namespace KRPC.ProtoBuf
{
    internal static class Encoder
    {
        /// <summary>
        /// Encode a value of the given type.
        /// </summary>
        public static byte[] Encode (object value, Type type)
        {
            var stream = new MemoryStream ();
            var encoder = new CodedOutputStream (stream);
            if (value != null && !type.IsAssignableFrom (value.GetType ())) //TODO: nulls?
                throw new ArgumentException ("Value of type " + value.GetType () + " cannot be encoded to type " + type);
            if (type == typeof(Double))
                encoder.WriteDouble ((Double)value);
            else if (type == typeof(Single))
                encoder.WriteFloat ((Single)value);
            else if (type == typeof(Int32))
                encoder.WriteInt32 ((Int32)value);
            else if (type == typeof(Int64))
                encoder.WriteInt64 ((Int64)value);
            else if (type == typeof(UInt32))
                encoder.WriteUInt32 ((UInt32)value);
            else if (type == typeof(UInt64))
                encoder.WriteUInt64 ((UInt64)value);
            else if (type == typeof(Boolean))
                encoder.WriteBool ((Boolean)value);
            else if (type == typeof(String))
                encoder.WriteString ((String)value);
            else if (type == typeof(byte[]))
                encoder.WriteBytes (ByteString.CopyFrom ((byte[])value));
            else if (value != null && value is Enum)
                encoder.WriteInt32 ((int)value);
            else if (TypeUtils.IsAClassType (type))
                encoder.WriteUInt64 (ObjectStore.Instance.AddInstance (value));
            else if (TypeUtils.IsAMessageType (type)) {
                IMessage message;
                if (type == typeof(Service.Messages.Status))
                    message = ((Service.Messages.Status)value).ToProtobufStatus ();
                else if (type == typeof(Service.Messages.Request))
                    message = ((Service.Messages.Request)value).ToProtobufRequest ();
                else if (type == typeof(Service.Messages.Response))
                    message = ((Service.Messages.Response)value).ToProtobufResponse ();
                else if (type == typeof(Service.Messages.Services))
                    message = ((Service.Messages.Services)value).ToProtobufServices ();
                else
                    throw new NotImplementedException (type + " is not supported");
                message.WriteTo (stream);
                return stream.ToArray ();
            } else if (TypeUtils.IsAListCollectionType (type))
                return EncodeList (value, type);
            else if (TypeUtils.IsADictionaryCollectionType (type))
                return EncodeDictionary (value, type);
            else if (TypeUtils.IsASetCollectionType (type))
                return EncodeSet (value, type);
            else if (TypeUtils.IsATupleCollectionType (type))
                return EncodeTuple (value, type);
            else
                throw new ArgumentException (type + " is not a serializable type");
            encoder.Flush ();
            return stream.ToArray ();
        }

        private static byte[] EncodeList (object value, Type type)
        {
            var encodedList = new KRPC.Schema.KRPC.List ();
            var list = (System.Collections.IList)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in list)
                encodedList.Items.Add (ByteString.CopyFrom (Encode (item, valueType)));
            return encodedList.ToByteArray ();
        }

        private static byte[] EncodeDictionary (object value, Type type)
        {
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var encodedDictionary = new KRPC.Schema.KRPC.Dictionary ();
            foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary) value) {
                var encodedEntry = new KRPC.Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = ByteString.CopyFrom (Encode (entry.Key, keyType));
                encodedEntry.Value = ByteString.CopyFrom (Encode (entry.Value, valueType));
                encodedDictionary.Entries.Add (encodedEntry);
            }
            return encodedDictionary.ToByteArray ();
        }

        private static byte[] EncodeSet (object value, Type type)
        {
            var encodedSet = new KRPC.Schema.KRPC.Set ();
            var set = (System.Collections.IEnumerable)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in set)
                encodedSet.Items.Add (ByteString.CopyFrom (Encode (item, valueType)));
            return encodedSet.ToByteArray ();
        }

        private static byte[] EncodeTuple (object value, Type type)
        {
            var encodedTuple = new KRPC.Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);
            var tupleType = genericType.MakeGenericType (valueTypes);
            for (int i = 0; i < valueTypes.Length; i++) {
                var property = tupleType.GetProperty ("Item" + (i + 1));
                var item = property.GetGetMethod ().Invoke (value, null);
                encodedTuple.Items.Add (ByteString.CopyFrom (Encode (item, valueTypes [i])));
            }
            return encodedTuple.ToByteArray ();
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static object Decode (byte[] value, Type type)
        {
            var stream = new CodedInputStream (value);
            if (type == typeof(double))
                return stream.ReadDouble ();
            else if (type == typeof(float))
                return stream.ReadFloat ();
            else if (type == typeof(int))
                return stream.ReadInt32 ();
            else if (type == typeof(long))
                return stream.ReadInt64 ();
            else if (type == typeof(uint))
                return stream.ReadUInt32 ();
            else if (type == typeof(ulong))
                return stream.ReadUInt64 ();
            else if (type == typeof(bool))
                return stream.ReadBool ();
            else if (type == typeof(string))
                return stream.ReadString ();
            else if (type == typeof(byte[]))
                return stream.ReadBytes ().ToByteArray ();
            else if (type.IsEnum)
                return stream.ReadInt32 ();
            else if (TypeUtils.IsAClassType (type))
                return ObjectStore.Instance.GetInstance (stream.ReadUInt64 ());
            else if (TypeUtils.IsAMessageType (type)) {
                if (type == typeof(Service.Messages.Request)) {
                    var message = new Schema.KRPC.Request ();
                    message.MergeFrom (stream);
                    return message.ToRequest ();
                } else if (type == typeof(Service.Messages.Response)) {
                    var message = new Schema.KRPC.Response ();
                    message.MergeFrom (stream);
                    return message.ToResponse ();
                } else
                    throw new NotImplementedException ("Cannot decode protobuf message for " + type);

            } else if (TypeUtils.IsAListCollectionType (type))
                return DecodeList (value, type);
            else if (TypeUtils.IsADictionaryCollectionType (type))
                return DecodeDictionary (value, type);
            else if (TypeUtils.IsASetCollectionType (type))
                return DecodeSet (value, type);
            // TODO: ugly handing of tuple types
            else if (TypeUtils.IsATupleCollectionType (type))
                return DecodeTuple (value, type);
            throw new ArgumentException (type + " is not a serializable type");
        }

        private static object DecodeList (byte[] value, Type type)
        {
            var encodedList = KRPC.Schema.KRPC.List.Parser.ParseFrom (value);
            var list = (System.Collections.IList)(typeof(System.Collections.Generic.List<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item.ToByteArray (), type.GetGenericArguments ().Single ()));
            return list;
        }

        private static object DecodeDictionary (byte[] value, Type type)
        {
            var encodedDictionary = KRPC.Schema.KRPC.Dictionary.Parser.ParseFrom (value);
            var dictionary = (System.Collections.IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var k = Decode (entry.Key.ToByteArray (), type.GetGenericArguments () [0]);
                var v = Decode (entry.Value.ToByteArray (), type.GetGenericArguments () [1]);
                dictionary [k] = v;
            }
            return dictionary;
        }

        private static object DecodeSet (byte[] value, Type type)
        {
            var encodedSet = KRPC.Schema.KRPC.Set.Parser.ParseFrom (value);
            var set = (System.Collections.IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = Decode (item.ToByteArray (), type.GetGenericArguments ().Single ());
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        private static object DecodeTuple (byte[] value, Type type)
        {
            var encodedTuple = KRPC.Schema.KRPC.Tuple.Parser.ParseFrom (value);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);
            Object[] values = new Object[valueTypes.Length];
            for (int j = 0; j < valueTypes.Length; j++) {
                var item = encodedTuple.Items [j];
                values [j] = Decode (item.ToByteArray (), valueTypes [j]);
            }
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }
    }
}
