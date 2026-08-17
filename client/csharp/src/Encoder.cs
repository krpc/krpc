using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// Methods for encoding and decoding messages for kRPCs protocolo bufers over TCP/IP protocol.
    /// </summary>
    public static class Encoder
    {
        /// <summary>
        /// Encode an object of the given type using the protocol buffer encoding scheme.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static ByteString Encode (object value, Type type)
        {
            // A Nullable<T> value is boxed to a plain T (or null), so encode it as its
            // underlying type; the null itself is signaled out-of-band by is_null.
            type = Nullable.GetUnderlyingType (type) ?? type;
            if (value == null)
                return null;
            return EncodeValue (value, type);
        }

        /// <summary>
        /// Encode a value of the given type.
        /// </summary>
        /// <remarks>
        /// A number, a string or an object identifier is a handful of bytes on the wire, and its
        /// bytes are written into a buffer the thread keeps and copied out of it. A coded output
        /// stream carries a buffer of several kilobytes of its own, which is far more to allocate
        /// for one number than writing the number costs. Anything carried by a protobuf message
        /// is left to protobuf to serialize.
        /// </remarks>
        static ByteString EncodeValue (object value, Type type)
        {
            if (value == null)
                throw new ArgumentException ("null cannot be encoded to type " + type);
            if (!type.IsInstanceOfType (value))
                throw new ArgumentException ("Value of type " + value.GetType () + " cannot be encoded to type " + type);
            if (value is Enum)
                return Varint (EncodeZigZag ((int)value));
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return Fixed64 ((ulong)BitConverter.DoubleToInt64Bits ((double)value));
            case TypeCode.Single:
                return Fixed32 (ToBits ((float)value));
            case TypeCode.Int32:
                return Varint (EncodeZigZag ((int)value));
            case TypeCode.Int64:
                return Varint (EncodeZigZag ((long)value));
            case TypeCode.UInt32:
                return Varint ((uint)value);
            case TypeCode.UInt64:
                return Varint ((ulong)value);
            case TypeCode.Boolean:
                return Varint ((bool)value ? 1UL : 0UL);
            case TypeCode.String:
                return EncodeString ((string)value);
            default:
                if (type.Equals (typeof(byte[])))
                    return EncodeBytes ((byte[])value);
                if (IsAClassType (type))
                    return Varint (((RemoteObject)value).id);
                if (IsATupleType (type))
                    return EncodeTuple (value, type).ToByteString ();
                if (IsAListType (type))
                    return EncodeList (value, type).ToByteString ();
                if (IsASetType (type))
                    return EncodeSet (value, type).ToByteString ();
                if (IsADictionaryType (type))
                    return EncodeDictionary (value, type).ToByteString ();
                if (IsAMessageType (type))
                    return ((IMessage)value).ToByteString ();
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static IMessage EncodeTuple (object value, Type type)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length.ToString (CultureInfo.InvariantCulture));
            var tupleType = genericType.MakeGenericType (valueTypes);
            for (int i = 0; i < valueTypes.Length; i++) {
                var property = tupleType.GetProperty ("Item" + (i + 1).ToString (CultureInfo.InvariantCulture));
                var item = property.GetGetMethod ().Invoke (value, null);
                encodedTuple.Items.Add (EncodeValue (item, valueTypes [i]));
            }
            return encodedTuple;
        }

        static IMessage EncodeList (object value, Type type)
        {
            var encodedList = new Schema.KRPC.List ();
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in (IList)value)
                encodedList.Items.Add (EncodeValue (item, valueType));
            return encodedList;
        }

        static IMessage EncodeSet (object value, Type type)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var valueType = type.GetGenericArguments ().Single ();
            foreach (var item in (IEnumerable)value)
                encodedSet.Items.Add (EncodeValue (item, valueType));
            return encodedSet;
        }

        static IMessage EncodeDictionary (object value, Type type)
        {
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            foreach (DictionaryEntry entry in (IDictionary)value) {
                var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = EncodeValue (entry.Key, keyType);
                encodedEntry.Value = EncodeValue (entry.Value, valueType);
                encodedDictionary.Entries.Add (encodedEntry);
            }
            return encodedDictionary;
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        /// <remarks>
        /// The bytes of a number, a string or an object identifier are read where they are, for
        /// the same reason the encoding writes them there: a coded input stream of its own is
        /// more to allocate for one value than reading the value costs.
        /// </remarks>
        public static object Decode (ByteString value, Type type, IConnection client)
        {
            if (ReferenceEquals (type, null))
                throw new ArgumentNullException (nameof (type));
            // A Nullable<T> value decodes as its underlying type T; a null value is
            // signaled out-of-band by is_null and never reaches this method.
            type = Nullable.GetUnderlyingType (type) ?? type;
            if (type.IsEnum)
                return DecodeZigZag32 (ReadVarint (value));
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return BitConverter.Int64BitsToDouble ((long)ReadFixed64 (value));
            case TypeCode.Single:
                return ToSingle (ReadFixed32 (value));
            case TypeCode.Int32:
                return DecodeZigZag32 (ReadVarint (value));
            case TypeCode.Int64:
                return DecodeZigZag64 (ReadVarint (value));
            case TypeCode.UInt32:
                return (uint)ReadVarint (value);
            case TypeCode.UInt64:
                return ReadVarint (value);
            case TypeCode.Boolean:
                return ReadVarint (value) != 0;
            case TypeCode.String:
                return value.CreateCodedInput ().ReadString ();
            default:
                if (type.Equals (typeof(byte[])))
                    return value.CreateCodedInput ().ReadBytes ().ToByteArray ();
                if (IsAClassType (type)) {
                    if (client == null)
                        throw new ArgumentException ("Client not passed when decoding remote object");
                    return (RemoteObject)Activator.CreateInstance (type, client, ReadVarint (value));
                }
                if (IsATupleType (type))
                    return DecodeTuple (value, type, client);
                if (IsAListType (type))
                    return DecodeList (value, type, client);
                if (IsASetType (type))
                    return DecodeSet (value, type, client);
                if (IsADictionaryType (type))
                    return DecodeDictionary (value, type, client);
                if (IsAMessageType (type)) {
                    var message = (IMessage)Activator.CreateInstance (type);
                    message.MergeFrom (value);
                    return message;
                }
                if (type == typeof (Event)) {
                    var message = new Schema.KRPC.Event ();
                    message.MergeFrom (value);
                    return new Event ((Connection)client, message);
                }
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static object DecodeTuple (ByteString data, Type type, IConnection client)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (data);
            var valueTypes = type.GetGenericArguments ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length.ToString (CultureInfo.InvariantCulture));
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

        static object DecodeList (ByteString data, Type type, IConnection client)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (data);
            var valueType = type.GetGenericArguments ().Single ();
            var list = (IList)(typeof(List<>)
                .MakeGenericType (valueType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, valueType, client));
            return list;
        }

        static object DecodeSet (ByteString data, Type type, IConnection client)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (data);
            var valueType = type.GetGenericArguments ().Single ();
            var set = (IEnumerable)(typeof(HashSet<>)
                .MakeGenericType (valueType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = Decode (item, valueType, client);
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        static object DecodeDictionary (ByteString data, Type type, IConnection client)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (data);
            var keyType = type.GetGenericArguments () [0];
            var valueType = type.GetGenericArguments () [1];
            var dictionary = (IDictionary)(typeof(Dictionary<,>)
                .MakeGenericType (keyType, valueType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var key = Decode (entry.Key, keyType, client);
                var value = Decode (entry.Value, valueType, client);
                dictionary [key] = value;
            }
            return dictionary;
        }

        /// <summary>
        /// A float and the bits it is made of, laid over each other so that one can be had from
        /// the other. The framework converts a double to its bits and back, and has nothing that
        /// does the same for a float.
        /// </summary>
        [StructLayout (LayoutKind.Explicit)]
        struct Single32
        {
            [FieldOffset (0)]
            public float Value;

            [FieldOffset (0)]
            public uint Bits;
        }

        static uint ToBits (float value)
        {
            var single = default (Single32);
            single.Value = value;
            return single.Bits;
        }

        static float ToSingle (uint bits)
        {
            var single = default (Single32);
            single.Bits = bits;
            return single.Value;
        }

        // The buffer a value's bytes are written into before being copied out of it, one per
        // thread. A value too big for it gets a buffer of its own rather than enlarging this one
        // for the rest of the thread's life.
        const int ScratchSize = 4096;

        // The longest a varint can be: ten bytes, for a 64 bit value.
        const int VarintSize = 10;

        [ThreadStatic]
        static byte[] scratch;

        static byte[] Scratch (int size)
        {
            if (size > ScratchSize)
                return new byte [size];
            if (scratch == null)
                scratch = new byte [ScratchSize];
            return scratch;
        }

        static ByteString Varint (ulong value)
        {
            var buffer = Scratch (VarintSize);
            return ByteString.CopyFrom (buffer, 0, WriteVarint (value, buffer, 0));
        }

        static int WriteVarint (ulong value, byte[] buffer, int offset)
        {
            var position = offset;
            while (value > 0x7f) {
                buffer [position++] = (byte)(value | 0x80);
                value >>= 7;
            }
            buffer [position++] = (byte)value;
            return position - offset;
        }

        static ByteString Fixed32 (uint bits)
        {
            var buffer = Scratch (4);
            for (var i = 0; i < 4; i++)
                buffer [i] = (byte)(bits >> (8 * i));
            return ByteString.CopyFrom (buffer, 0, 4);
        }

        static ByteString Fixed64 (ulong bits)
        {
            var buffer = Scratch (8);
            for (var i = 0; i < 8; i++)
                buffer [i] = (byte)(bits >> (8 * i));
            return ByteString.CopyFrom (buffer, 0, 8);
        }

        static ByteString EncodeString (string value)
        {
            var count = Encoding.UTF8.GetByteCount (value);
            var buffer = Scratch (count + VarintSize);
            var length = WriteVarint ((ulong)count, buffer, 0);
            Encoding.UTF8.GetBytes (value, 0, value.Length, buffer, length);
            return ByteString.CopyFrom (buffer, 0, length + count);
        }

        static ByteString EncodeBytes (byte[] value)
        {
            var buffer = Scratch (value.Length + VarintSize);
            var length = WriteVarint ((ulong)value.Length, buffer, 0);
            Buffer.BlockCopy (value, 0, buffer, length, value.Length);
            return ByteString.CopyFrom (buffer, 0, length + value.Length);
        }

        static ulong ReadVarint (ByteString data)
        {
            ulong value = 0;
            var shift = 0;
            // A varint that has not ended by the time it has filled a 64 bit value is not one,
            // and shifting any further would fold its bits back over the ones already read.
            for (var i = 0; i < data.Length && shift < 64; i++) {
                var b = data [i];
                value |= (ulong)(b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                    return value;
                shift += 7;
            }
            throw new ArgumentException ("Value is not a varint");
        }

        static uint ReadFixed32 (ByteString data)
        {
            if (data.Length < 4)
                throw new ArgumentException ("Value is not four bytes long");
            uint bits = 0;
            for (var i = 0; i < 4; i++)
                bits |= (uint)data [i] << (8 * i);
            return bits;
        }

        static ulong ReadFixed64 (ByteString data)
        {
            if (data.Length < 8)
                throw new ArgumentException ("Value is not eight bytes long");
            ulong bits = 0;
            for (var i = 0; i < 8; i++)
                bits |= (ulong)data [i] << (8 * i);
            return bits;
        }

        static ulong EncodeZigZag (int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        static ulong EncodeZigZag (long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        static int DecodeZigZag32 (ulong value)
        {
            var bits = (uint)value;
            return (int)(bits >> 1) ^ -(int)(bits & 1);
        }

        static long DecodeZigZag64 (ulong value)
        {
            return (long)(value >> 1) ^ -(long)(value & 1);
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
            IsAGenericType (type, typeof(Tuple<>)) ||
            IsAGenericType (type, typeof(Tuple<,>)) ||
            IsAGenericType (type, typeof(Tuple<,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,>)) ||
            IsAGenericType (type, typeof(Tuple<,,,,,,,>));
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
            return IsAGenericType (type, typeof(ISet<>));
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
