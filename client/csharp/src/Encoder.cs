using System;
using Google.Protobuf;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KRPC.Client
{
    internal static class Encoder
    {
        internal static readonly byte[] rpcHelloMessage = {
            0x48,
            0x45,
            0x4C,
            0x4C,
            0x4F,
            0x2D,
            0x52,
            0x50,
            0x43,
            0x00,
            0x00,
            0x00
        };
        internal static readonly byte[] streamHelloMessage = {
            0x48,
            0x45,
            0x4C,
            0x4C,
            0x4F,
            0x2D,
            0x53,
            0x54,
            0x52,
            0x45,
            0x41,
            0x4D
        };
        internal static readonly byte[] okMessage = { 0x4f, 0x4b };
        internal const int clientNameLength = 32;
        internal const int clientIdentifierLength = 16;

        internal static byte[] EncodeClientName (string name)
        {
            var encoder = new UTF8Encoding (false, true);
            var clientName = encoder.GetBytes (name);
            var clientNameBytes = new byte[clientNameLength];
            Array.Copy (clientName, clientNameBytes, Math.Min (clientNameLength, clientName.Length));
            return clientNameBytes;
        }

        internal static string ToHexString (byte[] data)
        {
            return "0x" + BitConverter.ToString (data).Replace ("-", " 0x");
        }

        public static ByteString Encode (object value, Type type)
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
            else if (type.IsSubclassOf (typeof(RemoteObject))) {
                if (value == null)
                    encoder.WriteUInt64 (0);
                else
                    encoder.WriteUInt64 (((RemoteObject)value)._ID);
            } else if (value != null && value is IMessage) {
                ((IMessage)value).WriteTo (stream);
                return ByteString.CopyFrom (stream.ToArray ());
            } else if (value != null && value is IList)
                return EncodeList (value, type);
            else if (value != null && value is IDictionary)
                return EncodeDictionary (value, type);
            else if (value != null && value.GetType ().IsGenericType && value.GetType ().GetGenericTypeDefinition () == typeof(HashSet<>))
                return EncodeSet (value, type); //TODO: ugly checking for set types
            else if (value != null && value.GetType ().IsGenericType &&
                     (value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<>) ||
                      value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<,>) ||
                      value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<,,>) ||
                      value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<,,,>) ||
                      value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<,,,,>) ||
                      value.GetType ().GetGenericTypeDefinition () == typeof(Tuple<,,,,,>)))
                return EncodeTuple (value, type); //TODO: ugly checking for tuple types
            else
                throw new ArgumentException (type + " is not a serializable type");
            encoder.Flush ();
            return ByteString.CopyFrom (stream.ToArray ());
        }

        private static ByteString EncodeList (object value, Type type)
        {
            var encodedList = new KRPC.Schema.KRPC.List ();
            var list = (System.Collections.IList)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in list)
                encodedList.Items.Add (Encode (item, valueType));
            return Encode (encodedList, typeof(KRPC.Schema.KRPC.List));
        }

        private static ByteString EncodeDictionary (object value, Type type)
        {
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var encodedDictionary = new KRPC.Schema.KRPC.Dictionary ();
            foreach (System.Collections.DictionaryEntry entry in (System.Collections.IDictionary) value) {
                var encodedEntry = new KRPC.Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = Encode (entry.Key, keyType);
                encodedEntry.Value = Encode (entry.Value, valueType);
                encodedDictionary.Entries.Add (encodedEntry);
            }
            return Encode (encodedDictionary, typeof(KRPC.Schema.KRPC.Dictionary));
        }

        private static ByteString EncodeSet (object value, Type type)
        {
            var encodedSet = new KRPC.Schema.KRPC.Set ();
            var set = (System.Collections.IEnumerable)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in set)
                encodedSet.Items.Add (Encode (item, valueType));
            return Encode (encodedSet, typeof(KRPC.Schema.KRPC.Set));
        }

        private static ByteString EncodeTuple (object value, Type type)
        {
            var encodedTuple = new KRPC.Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var tupleType = genericType.MakeGenericType (valueTypes);
            for (int i = 0; i < valueTypes.Length; i++) {
                var property = tupleType.GetProperty ("Item" + (i + 1));
                var item = property.GetGetMethod ().Invoke (value, null);
                encodedTuple.Items.Add (Encode (item, valueTypes [i]));
            }
            return Encode (encodedTuple, typeof(KRPC.Schema.KRPC.Tuple));
        }

        public static object Decode (ByteString value, Type type, Connection client)
        {
            var stream = new CodedInputStream (value.ToByteArray ());
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
            else if (typeof(RemoteObject).IsAssignableFrom (type)) {
                if (client == null)
                    throw new ArgumentException ("Client not passed when decoding remote object");
                var id = stream.ReadUInt64 ();
                if (id == 0)
                    return null;
                return (RemoteObject)Activator.CreateInstance (type, client, id);
            } else if (typeof(IMessage).IsAssignableFrom (type)) {
                IMessage message = (IMessage)Activator.CreateInstance (type);
                message.MergeFrom (stream);
                return message;
            } else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(IList<>))
                return DecodeList (value, type, client);
            else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(IDictionary<,>))
                return DecodeDictionary (value, type, client);
            else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof(ISet<>))
                return DecodeSet (value, type, client);
            else if (type.IsGenericType &&
                     (type.GetGenericTypeDefinition () == typeof(Tuple<>) ||
                      type.GetGenericTypeDefinition () == typeof(Tuple<,>) ||
                      type.GetGenericTypeDefinition () == typeof(Tuple<,,>) ||
                      type.GetGenericTypeDefinition () == typeof(Tuple<,,,>) ||
                      type.GetGenericTypeDefinition () == typeof(Tuple<,,,,>) ||
                      type.GetGenericTypeDefinition () == typeof(Tuple<,,,,,>)))
                return DecodeTuple (value, type, client); // TODO: ugly handing of tuple types
            throw new ArgumentException (type + " is not a serializable type");
        }

        private static object DecodeList (ByteString value, Type type, Connection client)
        {
            var encodedList = KRPC.Schema.KRPC.List.Parser.ParseFrom (value);
            var list = (System.Collections.IList)(typeof(System.Collections.Generic.List<>)
            .MakeGenericType (type.GetGenericArguments ().Single ())
            .GetConstructor (Type.EmptyTypes)
            .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, type.GetGenericArguments ().Single (), client));
            return list;
        }

        private static object DecodeDictionary (ByteString value, Type type, Connection client)
        {
            var encodedDictionary = KRPC.Schema.KRPC.Dictionary.Parser.ParseFrom (value);
            var dictionary = (System.Collections.IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var k = Decode (entry.Key, type.GetGenericArguments () [0], client);
                var v = Decode (entry.Value, type.GetGenericArguments () [1], client);
                dictionary [k] = v;
            }
            return dictionary;
        }

        private static object DecodeSet (ByteString value, Type type, Connection client)
        {
            var encodedSet = KRPC.Schema.KRPC.Set.Parser.ParseFrom (value);
            var set = (System.Collections.IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = Decode (item, type.GetGenericArguments ().Single (), client);
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        private static object DecodeTuple (ByteString value, Type type, Connection client)
        {
            var encodedTuple = KRPC.Schema.KRPC.Tuple.Parser.ParseFrom (value);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            Object[] values = new Object[valueTypes.Length];
            for (int j = 0; j < valueTypes.Length; j++) {
                var item = encodedTuple.Items [j];
                values [j] = Decode (item, valueTypes [j], client);
            }
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }
    }
}
