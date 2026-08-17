using System;
using System.Collections;
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
        /// bytes are written into a buffer of that size. A coded output stream carries a buffer
        /// of several kilobytes of its own, which is far more to allocate for one number than
        /// writing the number costs. Anything carried by a protobuf message
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
                return EncodeObject (value, type);
            }
        }

        /// <summary>
        /// Encode a value that is not a number, a string or an enumeration, from what is known
        /// about its type.
        /// </summary>
        static ByteString EncodeObject (object value, Type type)
        {
            var info = TypeInfo.For (type);
            switch (info.Kind) {
            case TypeKind.Bytes:
                return EncodeBytes ((byte[])value);
            case TypeKind.Class:
                return Varint (((RemoteObject)value).id);
            case TypeKind.Tuple:
                return EncodeTuple (value, info).ToByteString ();
            case TypeKind.List:
                return EncodeList (value, info).ToByteString ();
            case TypeKind.Set:
                return EncodeSet (value, info).ToByteString ();
            case TypeKind.Dictionary:
                return EncodeDictionary (value, info).ToByteString ();
            case TypeKind.Message:
                return ((IMessage)value).ToByteString ();
            default:
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static IMessage EncodeTuple (object value, TypeInfo info)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            for (int i = 0; i < info.Arguments.Length; i++)
                encodedTuple.Items.Add (EncodeValue (info.Items [i] (value), info.Arguments [i]));
            return encodedTuple;
        }

        static IMessage EncodeList (object value, TypeInfo info)
        {
            var encodedList = new Schema.KRPC.List ();
            var valueType = info.Arguments [0];
            foreach (var item in (IList)value)
                encodedList.Items.Add (EncodeValue (item, valueType));
            return encodedList;
        }

        static IMessage EncodeSet (object value, TypeInfo info)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var valueType = info.Arguments [0];
            foreach (var item in (IEnumerable)value)
                encodedSet.Items.Add (EncodeValue (item, valueType));
            return encodedSet;
        }

        static IMessage EncodeDictionary (object value, TypeInfo info)
        {
            var keyType = info.Arguments [0];
            var valueType = info.Arguments [1];
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
                return DecodeObject (value, type, client);
            }
        }

        /// <summary>
        /// Decode a value that is not a number, a string or an enumeration, from what is known
        /// about its type.
        /// </summary>
        static object DecodeObject (ByteString value, Type type, IConnection client)
        {
            var info = TypeInfo.For (type);
            switch (info.Kind) {
            case TypeKind.Bytes:
                return value.CreateCodedInput ().ReadBytes ().ToByteArray ();
            case TypeKind.Class:
                if (client == null)
                    throw new ArgumentException ("Client not passed when decoding remote object");
                return info.NewObject (client, ReadVarint (value));
            case TypeKind.Tuple:
                return DecodeTuple (value, info, client);
            case TypeKind.List:
                return DecodeList (value, info, client);
            case TypeKind.Set:
                return DecodeSet (value, info, client);
            case TypeKind.Dictionary:
                return DecodeDictionary (value, info, client);
            case TypeKind.Message: {
                    var message = (IMessage)Activator.CreateInstance (type);
                    message.MergeFrom (value);
                    return message;
                }
            case TypeKind.Event: {
                    var message = new Schema.KRPC.Event ();
                    message.MergeFrom (value);
                    return new Event ((Connection)client, message);
                }
            default:
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static object DecodeTuple (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (data);
            var values = new object [info.Arguments.Length];
            for (int i = 0; i < values.Length; i++)
                values [i] = Decode (encodedTuple.Items [i], info.Arguments [i], client);
            return info.NewTuple (values);
        }

        static object DecodeList (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (data);
            var valueType = info.Arguments [0];
            var list = (IList)info.New ();
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, valueType, client));
            return list;
        }

        static object DecodeSet (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (data);
            var valueType = info.Arguments [0];
            var set = info.New ();
            foreach (var item in encodedSet.Items)
                info.Add (set, Decode (item, valueType, client));
            return set;
        }

        static object DecodeDictionary (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (data);
            var keyType = info.Arguments [0];
            var valueType = info.Arguments [1];
            var dictionary = (IDictionary)info.New ();
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

        // The longest a varint can be: ten bytes, for a 64 bit value.
        const int VarintSize = 10;

        // A value's bytes are written into a buffer of their own, made where they are written.
        // Keeping one buffer per thread instead was measured and is far worse: reaching a
        // thread's own static costs several times what making a buffer this small costs, and it
        // is reached once for every value written.

        static ByteString Varint (ulong value)
        {
            var buffer = new byte [VarintSize];
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
            var buffer = new byte [4];
            for (var i = 0; i < 4; i++)
                buffer [i] = (byte)(bits >> (8 * i));
            return ByteString.CopyFrom (buffer, 0, 4);
        }

        static ByteString Fixed64 (ulong bits)
        {
            var buffer = new byte [8];
            for (var i = 0; i < 8; i++)
                buffer [i] = (byte)(bits >> (8 * i));
            return ByteString.CopyFrom (buffer, 0, 8);
        }

        static ByteString EncodeString (string value)
        {
            var count = Encoding.UTF8.GetByteCount (value);
            var buffer = new byte [count + VarintSize];
            var length = WriteVarint ((ulong)count, buffer, 0);
            Encoding.UTF8.GetBytes (value, 0, value.Length, buffer, length);
            return ByteString.CopyFrom (buffer, 0, length + count);
        }

        static ByteString EncodeBytes (byte[] value)
        {
            var buffer = new byte [value.Length + VarintSize];
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
    }
}
