using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;

#if NET35
using systemAlias = global::KRPC.Client.Compatibility;
using genericCollectionsAlias = global::KRPC.Client.Compatibility;
#else
using systemAlias = global::System;
using genericCollectionsAlias = global::System.Collections.Generic;
#endif

namespace KRPC.Client
{
    /// <summary>
    /// Methods for encoding and decoding messages for kRPCs protocolo bufers over TCP/IP protocol.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    public static class Encoder
    {
        /// <summary>
        /// Encode an object of the given type using the protocol buffer encoding scheme.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static ByteString Encode (object value, Type type)
        {
            using (var buffer = new MemoryStream ()) {
                var stream = new CodedOutputStream (buffer, true);
                return EncodeObject (value, type, buffer, stream);
            }
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnneededUnboxingRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        static ByteString EncodeObject (object value, Type type, MemoryStream buffer, CodedOutputStream stream)
        {
            buffer.SetLength (0);
            if (value != null && !type.IsInstanceOfType (value))
                throw new ArgumentException ("Value of type " + value.GetType () + " cannot be encoded to type " + type);
            if (value == null && !type.IsSubclassOf (typeof(RemoteObject)) && !IsACollectionType (type))
                throw new ArgumentException ("null cannot be encoded to type " + type);
            if (value == null)
                stream.WriteUInt64 (0);
            else if (value is Enum)
                stream.WriteSInt32 ((int)value);
            else {
                switch (Type.GetTypeCode (type)) {
                case TypeCode.Double:
                    stream.WriteDouble ((double)value);
                    break;
                case TypeCode.Single:
                    stream.WriteFloat ((float)value);
                    break;
                case TypeCode.Int32:
                    stream.WriteSInt32 ((int)value);
                    break;
                case TypeCode.Int64:
                    stream.WriteSInt64 ((long)value);
                    break;
                case TypeCode.UInt32:
                    stream.WriteUInt32 ((uint)value);
                    break;
                case TypeCode.UInt64:
                    stream.WriteUInt64 ((ulong)value);
                    break;
                case TypeCode.Boolean:
                    stream.WriteBool ((bool)value);
                    break;
                case TypeCode.String:
                    stream.WriteString ((string)value);
                    break;
                default:
                    if (type.Equals (typeof(byte[])))
                        stream.WriteBytes (ByteString.CopyFrom ((byte[])value));
                    else if (IsAClassType (type))
                        stream.WriteUInt64 (((RemoteObject)value).id);
                    else if (IsATupleType (type))
                        WriteTuple (value, type, buffer);
                    else if (IsAListType (type))
                        WriteList (value, type, buffer);
                    else if (IsASetType (type))
                        WriteSet (value, type, buffer);
                    else if (IsADictionaryType (type))
                        WriteDictionary (value, type, buffer);
                    else if (IsAMessageType (type))
                        ((IMessage)value).WriteTo (buffer);
                    else
                        throw new ArgumentException (type + " is not a serializable type");
                    break;
                }
            }
            stream.Flush ();
            return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        static void WriteTuple (object value, Type type, Stream stream)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length.ToString ());
            var tupleType = genericType.MakeGenericType (valueTypes);
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                for (int i = 0; i < valueTypes.Length; i++) {
                    var property = tupleType.GetProperty ("Item" + (i + 1).ToString ());
                    var item = property.GetGetMethod ().Invoke (value, null);
                    encodedTuple.Items.Add (EncodeObject (item, valueTypes [i], internalBuffer, internalStream));
                }
            }
            encodedTuple.WriteTo (stream);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        static void WriteList (object value, Type type, Stream stream)
        {
            var encodedList = new Schema.KRPC.List ();
            var list = (IList)value;
            var valueType = type.GetGenericArguments ().Single ();
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in list)
                    encodedList.Items.Add (EncodeObject (item, valueType, internalBuffer, internalStream));
            }
            encodedList.WriteTo (stream);
        }

        static void WriteSet (object value, Type type, Stream stream)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var set = (IEnumerable)value;
            var valueType = type.GetGenericArguments ().Single ();
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in set)
                    encodedSet.Items.Add (EncodeObject (item, valueType, internalBuffer, internalStream));
            }
            encodedSet.WriteTo (stream);
        }

        static void WriteDictionary (object value, Type type, Stream stream)
        {
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (DictionaryEntry entry in (IDictionary) value) {
                    var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                    encodedEntry.Key = EncodeObject (entry.Key, keyType, internalBuffer, internalStream);
                    encodedEntry.Value = EncodeObject (entry.Value, valueType, internalBuffer, internalStream);
                    encodedDictionary.Entries.Add (encodedEntry);
                }
            }
            encodedDictionary.WriteTo (stream);
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static object Decode (ByteString value, Type type, IConnection client)
        {
            if (type == null)
                throw new ArgumentNullException (nameof (type));
            var stream = value.CreateCodedInput ();
            if (type.IsEnum)
                return stream.ReadSInt32 ();
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return stream.ReadDouble ();
            case TypeCode.Single:
                return stream.ReadFloat ();
            case TypeCode.Int32:
                return stream.ReadSInt32 ();
            case TypeCode.Int64:
                return stream.ReadSInt64 ();
            case TypeCode.UInt32:
                return stream.ReadUInt32 ();
            case TypeCode.UInt64:
                return stream.ReadUInt64 ();
            case TypeCode.Boolean:
                return stream.ReadBool ();
            case TypeCode.String:
                return stream.ReadString ();
            default:
                if (type.Equals (typeof(byte[])))
                    return stream.ReadBytes ().ToByteArray ();
                if (IsAClassType (type)) {
                    if (client == null)
                        throw new ArgumentException ("Client not passed when decoding remote object");
                    var id = stream.ReadUInt64 ();
                    return id == 0 ? null : (RemoteObject)Activator.CreateInstance (type, client, id);
                }
                if (IsATupleType (type))
                    return DecodeTuple (stream, type, client);
                if (IsAListType (type))
                    return DecodeList (stream, type, client);
                if (IsASetType (type))
                    return DecodeSet (stream, type, client);
                if (IsADictionaryType (type))
                    return DecodeDictionary (stream, type, client);
                if (IsAMessageType (type)) {
                    var message = (IMessage)Activator.CreateInstance (type);
                    message.MergeFrom (stream);
                    return message;
                }
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static object DecodeTuple (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (stream);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length.ToString ());
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

        static object DecodeList (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (stream);
            var list = (IList)(typeof(List<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, type.GetGenericArguments ().Single (), client));
            return list;
        }

        static object DecodeSet (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (stream);
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

        static object DecodeDictionary (CodedInputStream stream, Type type, IConnection client)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (stream);
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

        static bool IsAGenericType (Type type, Type genericType)
        {
            while (!ReferenceEquals (type, null)) {
                if (type.IsGenericType && type.GetGenericTypeDefinition ().Equals (genericType))
                    return true;
                foreach (var intType in type.GetInterfaces())
                    if (IsAGenericType (intType, genericType))
                        return true;
                type = type.BaseType;
            }
            return false;
        }

        static bool IsAClassType (Type type)
        {
            return type.IsSubclassOf (typeof(RemoteObject));
        }

        static bool IsATupleType (Type type)
        {
            return
            IsAGenericType (type, typeof(systemAlias::Tuple<>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,,,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,,,,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,,,,,>)) ||
            IsAGenericType (type, typeof(systemAlias::Tuple<,,,,,,,>));
        }

        static bool IsACollectionType (Type type)
        {
            return IsATupleType (type) || IsAListType (type) || IsASetType (type) || IsADictionaryType (type);
        }

        static bool IsAListType (Type type)
        {
            return IsAGenericType (type, typeof(IList<>));
        }

        static bool IsASetType (Type type)
        {
            return IsAGenericType (type, typeof(genericCollectionsAlias::ISet<>));
        }

        static bool IsADictionaryType (Type type)
        {
            return IsAGenericType (type, typeof(IDictionary<,>));
        }

        static bool IsAMessageType (Type type)
        {
            return typeof(IMessage).IsAssignableFrom (type);
        }
    }
}
