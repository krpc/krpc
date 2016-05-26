using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Google.Protobuf;

namespace KRPC.Client
{
    static class Encoder
    {
        public static readonly byte[] RPCHelloMessage = {
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
        public static readonly byte[] StreamHelloMessage = {
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
        public static readonly byte[] OkMessage = { 0x4f, 0x4b };
        public const int ClientNameLength = 32;
        public const int ClientIdentifierLength = 16;

        internal static byte[] EncodeClientName (string name)
        {
            var encoder = new UTF8Encoding (false, true);
            var clientName = encoder.GetBytes (name);
            var clientNameBytes = new byte[ClientNameLength];
            Array.Copy (clientName, clientNameBytes, Math.Min (ClientNameLength, clientName.Length));
            return clientNameBytes;
        }

        internal static string ToHexString (byte[] data)
        {
            return "0x" + BitConverter.ToString (data).Replace ("-", " 0x");
        }

        static bool IsAGenericType (Type type, Type genericType)
        {
            while (type != null) {
                if (type.IsGenericType && type.GetGenericTypeDefinition () == genericType)
                    return true;
                foreach (var intType in type.GetInterfaces())
                    if (IsAGenericType (intType, genericType))
                        return true;
                type = type.BaseType;
            }
            return false;
        }

        static MemoryStream cachedBuffer = new MemoryStream ();
        static CodedOutputStream cachedStream = new CodedOutputStream (cachedBuffer);

        /// <summary>
        /// Encode an object of the given type using the protocol buffer encoding scheme.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static ByteString Encode (object value, Type type)
        {
            return EncodeObject (value, type, cachedBuffer, cachedStream);
        }

        static ByteString EncodeObject (object value, Type type, MemoryStream buffer, CodedOutputStream stream)
        {
            buffer.SetLength (0);
            if (value != null && !type.IsInstanceOfType (value))
                throw new ArgumentException ("Value of type " + value.GetType () + " cannot be encoded to type " + type);
            if (value == null && !type.IsSubclassOf (typeof(RemoteObject)))
                throw new ArgumentException ("null cannot be encoded to type " + type);
            if (value == null) {
                stream.WriteUInt64 (0);
                stream.Flush ();
                return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
            }
            if (type == typeof(Double))
                stream.WriteDouble ((Double)value);
            else if (type == typeof(Single))
                stream.WriteFloat ((Single)value);
            else if (type == typeof(Int32))
                stream.WriteInt32 ((Int32)value);
            else if (type == typeof(Int64))
                stream.WriteInt64 ((Int64)value);
            else if (type == typeof(UInt32))
                stream.WriteUInt32 ((UInt32)value);
            else if (type == typeof(UInt64))
                stream.WriteUInt64 ((UInt64)value);
            else if (type == typeof(Boolean))
                stream.WriteBool ((Boolean)value);
            else if (type == typeof(String))
                stream.WriteString ((String)value);
            else if (type == typeof(byte[]))
                stream.WriteBytes (ByteString.CopyFrom ((byte[])value));
            else if (value is Enum)
                stream.WriteInt32 ((int)value);
            else if (type.IsSubclassOf (typeof(RemoteObject)))
                stream.WriteUInt64 (((RemoteObject)value)._ID);
            else if ((value as IMessage) != null)
                ((IMessage)value).WriteTo (buffer);
            else if (IsAListType (type))
                WriteList (value, type, buffer);
            else if (IsADictionaryType (type))
                WriteDictionary (value, type, buffer);
            else if (IsASetType (type))
                WriteSet (value, type, buffer);
            else if (IsATupleType (type))
                WriteTuple (value, type, buffer);
            else
                throw new ArgumentException (type + " is not a serializable type");
            stream.Flush ();
            return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
        }

        static bool IsAListType (Type type)
        {
            return IsAGenericType (type, typeof(IList<>));
        }

        static bool IsADictionaryType (Type type)
        {
            return IsAGenericType (type, typeof(IDictionary<,>));
        }

        static bool IsASetType (Type type)
        {
            return IsAGenericType (type, typeof(ISet<>));
        }

        static bool IsATupleType (Type type)
        {
            return
            IsAGenericType (type, typeof(Tuple<>)) ||
            IsAGenericType (type, typeof(Tuple<,>)) ||
            IsAGenericType (type, typeof(Tuple<,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,,>));
        }

        static void WriteList (object value, Type type, Stream stream)
        {
            var internalBuffer = new MemoryStream ();
            var internalStream = new CodedOutputStream (internalBuffer);
            var encodedList = new KRPC.Schema.KRPC.List ();
            var list = (IList)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in list)
                encodedList.Items.Add (EncodeObject (item, valueType, internalBuffer, internalStream));
            encodedList.WriteTo (stream);
        }

        static void WriteDictionary (object value, Type type, Stream stream)
        {
            var internalBuffer = new MemoryStream ();
            var internalStream = new CodedOutputStream (internalBuffer);
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var encodedDictionary = new KRPC.Schema.KRPC.Dictionary ();
            foreach (DictionaryEntry entry in (IDictionary) value) {
                var encodedEntry = new KRPC.Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = EncodeObject (entry.Key, keyType, internalBuffer, internalStream);
                encodedEntry.Value = EncodeObject (entry.Value, valueType, internalBuffer, internalStream);
                encodedDictionary.Entries.Add (encodedEntry);
            }
            encodedDictionary.WriteTo (stream);
        }

        static void WriteSet (object value, Type type, Stream stream)
        {
            var internalBuffer = new MemoryStream ();
            var internalStream = new CodedOutputStream (internalBuffer);
            var encodedSet = new KRPC.Schema.KRPC.Set ();
            var set = (IEnumerable)value;
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in set)
                encodedSet.Items.Add (EncodeObject (item, valueType, internalBuffer, internalStream));
            encodedSet.WriteTo (stream);
        }

        static void WriteTuple (object value, Type type, Stream stream)
        {
            var internalBuffer = new MemoryStream ();
            var internalStream = new CodedOutputStream (internalBuffer);
            var encodedTuple = new KRPC.Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var tupleType = genericType.MakeGenericType (valueTypes);
            for (int i = 0; i < valueTypes.Length; i++) {
                var property = tupleType.GetProperty ("Item" + (i + 1));
                var item = property.GetGetMethod ().Invoke (value, null);
                encodedTuple.Items.Add (EncodeObject (item, valueTypes [i], internalBuffer, internalStream));
            }
            encodedTuple.WriteTo (stream);
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static object Decode (ByteString value, Type type, IConnection client)
        {
            var stream = value.CreateCodedInput ();
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
                return id == 0 ? null : (RemoteObject)Activator.CreateInstance (type, client, id);
            } else if (typeof(IMessage).IsAssignableFrom (type)) {
                var message = (IMessage)Activator.CreateInstance (type);
                message.MergeFrom (stream);
                return message;
            } else if (IsAListType (type))
                return DecodeList (stream, type, client);
            else if (IsADictionaryType (type))
                return DecodeDictionary (stream, type, client);
            else if (IsASetType (type))
                return DecodeSet (stream, type, client);
            else if (IsATupleType (type))
                return DecodeTuple (stream, type, client);
            throw new ArgumentException (type + " is not a serializable type");
        }

        static object DecodeList (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedList = KRPC.Schema.KRPC.List.Parser.ParseFrom (stream);
            var list = (IList)(typeof(List<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, type.GetGenericArguments ().Single (), client));
            return list;
        }

        static object DecodeDictionary (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedDictionary = KRPC.Schema.KRPC.Dictionary.Parser.ParseFrom (stream);
            var dictionary = (IDictionary)(typeof(Dictionary<,>)
                .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var key = Decode (entry.Key, type.GetGenericArguments () [0], client);
                var value = Decode (entry.Value, type.GetGenericArguments () [1], client);
                dictionary [key] = value;
            }
            return dictionary;
        }

        static object DecodeSet (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedSet = KRPC.Schema.KRPC.Set.Parser.ParseFrom (stream);
            var set = (IEnumerable)(typeof(HashSet<>)
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

        static object DecodeTuple (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedTuple = KRPC.Schema.KRPC.Tuple.Parser.ParseFrom (stream);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var values = new object[valueTypes.Length];
            for (int i = 0; i < valueTypes.Length; i++) {
                var item = encodedTuple.Items [i];
                values [i] = Decode (item, valueTypes [i], client);
            }
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }
    }
}
