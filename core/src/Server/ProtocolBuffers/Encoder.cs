using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using KRPC.Service;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    static class Encoder
    {
        static MemoryStream cachedBuffer = new MemoryStream ();
        static CodedOutputStream cachedStream = new CodedOutputStream (cachedBuffer);

        /// <summary>
        /// Encode an object using the protocol buffer encoding scheme.
        /// </summary>
        public static ByteString Encode (object value)
        {
            return EncodeObject (value, cachedBuffer, cachedStream);
        }

        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidUnneededUnboxingRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        static ByteString EncodeObject (object value, MemoryStream buffer, CodedOutputStream stream)
        {
            buffer.SetLength (0);
            if (value == null) {
                stream.WriteUInt64 (0);
            } else if (value is Enum) {
                stream.WriteSInt32 ((int)value);
            } else {
                var type = value.GetType ();
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
                    if (type == typeof(byte[]))
                        stream.WriteBytes (ByteString.CopyFrom ((byte[])value));
                    else if (TypeUtils.IsAClassType (type))
                        stream.WriteUInt64 (ObjectStore.Instance.AddInstance (value));
                    else if (TypeUtils.IsATupleCollectionType (type))
                        WriteTuple (value, stream);
                    else if (TypeUtils.IsAListCollectionType (type))
                        WriteList (value, stream);
                    else if (TypeUtils.IsASetCollectionType (type))
                        WriteSet (value, stream);
                    else if (TypeUtils.IsADictionaryCollectionType (type))
                        WriteDictionary (value, stream);
                    else if (TypeUtils.IsAMessageType (type))
                        WriteMessage (value, stream);
                    else
                        throw new ArgumentException (type + " is not a serializable type");
                    break;
                }
            }
            stream.Flush ();
            return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
        }

        static void WriteTuple (object value, CodedOutputStream stream)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            var valueTypes = value.GetType ().GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var tupleType = genericType.MakeGenericType (valueTypes);
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                for (int i = 0; i < valueTypes.Length; i++) {
                    var property = tupleType.GetProperty ("Item" + (i + 1));
                    var item = property.GetGetMethod ().Invoke (value, null);
                    encodedTuple.Items.Add (EncodeObject (item, internalBuffer, internalStream));
                }
            }
            encodedTuple.WriteTo (stream);
        }

        static void WriteList (object value, CodedOutputStream stream)
        {
            var encodedList = new Schema.KRPC.List ();
            var list = (IList)value;
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in list)
                    encodedList.Items.Add (EncodeObject (item, internalBuffer, internalStream));
            }
            encodedList.WriteTo (stream);
        }

        static void WriteSet (object value, CodedOutputStream stream)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var set = (IEnumerable)value;
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in set)
                    encodedSet.Items.Add (EncodeObject (item, internalBuffer, internalStream));
            }
            encodedSet.WriteTo (stream);
        }

        static void WriteDictionary (object value, CodedOutputStream stream)
        {
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (DictionaryEntry entry in (IDictionary) value) {
                    var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                    encodedEntry.Key = EncodeObject (entry.Key, internalBuffer, internalStream);
                    encodedEntry.Value = EncodeObject (entry.Value, internalBuffer, internalStream);
                    encodedDictionary.Entries.Add (encodedEntry);
                }
            }
            encodedDictionary.WriteTo (stream);
        }

        static void WriteMessage (object value, CodedOutputStream stream)
        {
            var savedCachedBuffer = cachedBuffer;
            var savedCachedStream = cachedStream;
            cachedBuffer = new MemoryStream ();
            cachedStream = new CodedOutputStream (cachedBuffer);
            Google.Protobuf.IMessage message = ((Service.Messages.IMessage)value).ToProtobufMessage ();
            cachedBuffer = savedCachedBuffer;
            cachedStream = savedCachedStream;
            message.WriteTo (stream);
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static object Decode (ByteString value, Type type)
        {
            var stream = value.CreateCodedInput ();
            if (type.IsEnum) {
                if (TypeUtils.IsAnEnumType (type))
                    return Enum.ToObject (type, stream.ReadSInt32 ());
            } else {
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
                    if (type == typeof(byte[]))
                        return stream.ReadBytes ().ToByteArray ();
                    if (TypeUtils.IsAClassType (type))
                        return ObjectStore.Instance.GetInstance (stream.ReadUInt64 ());
                    if (TypeUtils.IsATupleCollectionType (type))
                        return DecodeTuple (stream, type);
                    if (TypeUtils.IsAListCollectionType (type))
                        return DecodeList (stream, type);
                    if (TypeUtils.IsASetCollectionType (type))
                        return DecodeSet (stream, type);
                    if (TypeUtils.IsADictionaryCollectionType (type))
                        return DecodeDictionary (stream, type);
                    if (TypeUtils.IsAMessageType (type))
                        return DecodeMessage (stream, type);
                    break;
                }
            }
            throw new ArgumentException (type + " is not a serializable type");
        }

        static object DecodeTuple (CodedInputStream stream, Type type)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (stream);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var values = new object[valueTypes.Length];
            for (int i = 0; i < valueTypes.Length; i++) {
                var item = encodedTuple.Items [i];
                values [i] = Decode (item, valueTypes [i]);
            }
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }

        static object DecodeList (CodedInputStream stream, Type type)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (stream);
            var list = (IList)(typeof(System.Collections.Generic.List<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, type.GetGenericArguments ().Single ()));
            return list;
        }

        static object DecodeSet (CodedInputStream stream, Type type)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (stream);
            var set = (IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = Decode (item, type.GetGenericArguments ().Single ());
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        static object DecodeDictionary (CodedInputStream stream, Type type)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (stream);
            var dictionary = (IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var key = Decode (entry.Key, type.GetGenericArguments () [0]);
                var value = Decode (entry.Value, type.GetGenericArguments () [1]);
                dictionary [key] = value;
            }
            return dictionary;
        }

        static object DecodeMessage (CodedInputStream stream, Type type)
        {
            if (type == typeof(ProcedureCall)) {
                var message = new Schema.KRPC.ProcedureCall ();
                message.MergeFrom (stream);
                return message.ToMessage ();
            }
            throw new ArgumentException ("Cannot decode protocol buffer messages of type " + type);
        }
    }
}
