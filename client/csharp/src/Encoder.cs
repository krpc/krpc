using System;
using System.Collections;
using System.Collections.Concurrent;
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
        /// writing the number costs. Anything carried by a protobuf message is left to protobuf
        /// to serialize.
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
            case TypeKind.Struct:
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

        static readonly ConcurrentDictionary<Type, Func<object, ByteString>> encoders =
            new ConcurrentDictionary<Type, Func<object, ByteString>> ();

        /// <summary>
        /// A function that encodes one value of the given type.
        /// </summary>
        /// <remarks>
        /// A collection carries many values of the same type, and reaching the code that writes
        /// one of them means asking the type whether it is an enumeration and which type code it
        /// carries, and for anything that is not a number or a string, what kind of value it is.
        /// None of those answers change, so a collection asks once and calls what it gets back
        /// for each of its items.
        /// </remarks>
        static Func<object, ByteString> EncoderFor (Type type)
        {
            Func<object, ByteString> encode;
            if (encoders.TryGetValue (type, out encode))
                return encode;
            return encoders.GetOrAdd (type, BuildEncoder (type));
        }

        /// <remarks>
        /// A value reached this way is an item of a collection whose type says what its items
        /// are, so unlike <see cref="EncodeValue"/> this does not ask each one whether it is of
        /// that type. Asking is a call into the type system, and asking it once per item is what
        /// made writing a collection cost several times what reading one back costs. A null is
        /// still rejected, as a collection of a class type can hold one.
        /// </remarks>
        static Func<object, ByteString> BuildEncoder (Type type)
        {
            Func<object, ByteString> write = WriterFor (type);
            return value => {
                if (value == null)
                    throw new ArgumentException ("null cannot be encoded to type " + type);
                return write (value);
            };
        }

        /// <summary>
        /// A function that writes one value of the given type, without checking it first.
        /// </summary>
        static Func<object, ByteString> WriterFor (Type type)
        {
            if (type.IsEnum)
                return value => Varint (EncodeZigZag ((int)value));
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return value => Fixed64 ((ulong)BitConverter.DoubleToInt64Bits ((double)value));
            case TypeCode.Single:
                return value => Fixed32 (ToBits ((float)value));
            case TypeCode.Int32:
                return value => Varint (EncodeZigZag ((int)value));
            case TypeCode.Int64:
                return value => Varint (EncodeZigZag ((long)value));
            case TypeCode.UInt32:
                return value => Varint ((uint)value);
            case TypeCode.UInt64:
                return value => Varint ((ulong)value);
            case TypeCode.Boolean:
                return value => Varint ((bool)value ? 1UL : 0UL);
            case TypeCode.String:
                return value => EncodeString ((string)value);
            default:
                break;
            }
            var info = TypeInfo.For (type);
            switch (info.Kind) {
            case TypeKind.Bytes:
                return value => EncodeBytes ((byte[])value);
            case TypeKind.Class:
                return value => Varint (((RemoteObject)value).id);
            case TypeKind.Struct:
            case TypeKind.Tuple:
                return value => EncodeTuple (value, info).ToByteString ();
            case TypeKind.List:
                return value => EncodeList (value, info).ToByteString ();
            case TypeKind.Set:
                return value => EncodeSet (value, info).ToByteString ();
            case TypeKind.Dictionary:
                return value => EncodeDictionary (value, info).ToByteString ();
            case TypeKind.Message:
                return value => ((IMessage)value).ToByteString ();
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
            var encodeItem = EncoderFor (info.Arguments [0]);
            foreach (var item in (IList)value)
                encodedList.Items.Add (encodeItem (item));
            return encodedList;
        }

        static IMessage EncodeSet (object value, TypeInfo info)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var encodeItem = EncoderFor (info.Arguments [0]);
            foreach (var item in (IEnumerable)value)
                encodedSet.Items.Add (encodeItem (item));
            return encodedSet;
        }

        static IMessage EncodeDictionary (object value, TypeInfo info)
        {
            var encodeKey = EncoderFor (info.Arguments [0]);
            var encodeValue = EncoderFor (info.Arguments [1]);
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            foreach (DictionaryEntry entry in (IDictionary)value) {
                var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = encodeKey (entry.Key);
                encodedEntry.Value = encodeValue (entry.Value);
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
            case TypeKind.Struct:
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

        static readonly ConcurrentDictionary<Type, Func<ByteString, IConnection, object>> decoders =
            new ConcurrentDictionary<Type, Func<ByteString, IConnection, object>> ();

        /// <summary>
        /// A function that decodes one value of the given type. The counterpart of
        /// <see cref="EncoderFor"/>, and there for the same reason.
        /// </summary>
        static Func<ByteString, IConnection, object> DecoderFor (Type type)
        {
            Func<ByteString, IConnection, object> decode;
            if (decoders.TryGetValue (type, out decode))
                return decode;
            return decoders.GetOrAdd (type, BuildDecoder (type));
        }

        static Func<ByteString, IConnection, object> BuildDecoder (Type type)
        {
            // A Nullable<T> value decodes as its underlying type T; a null value is
            // signaled out-of-band by is_null and never reaches here.
            type = Nullable.GetUnderlyingType (type) ?? type;
            if (type.IsEnum)
                return (value, client) => DecodeZigZag32 (ReadVarint (value));
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return (value, client) => BitConverter.Int64BitsToDouble ((long)ReadFixed64 (value));
            case TypeCode.Single:
                return (value, client) => ToSingle (ReadFixed32 (value));
            case TypeCode.Int32:
                return (value, client) => DecodeZigZag32 (ReadVarint (value));
            case TypeCode.Int64:
                return (value, client) => DecodeZigZag64 (ReadVarint (value));
            case TypeCode.UInt32:
                return (value, client) => (uint)ReadVarint (value);
            case TypeCode.UInt64:
                return (value, client) => ReadVarint (value);
            case TypeCode.Boolean:
                return (value, client) => ReadVarint (value) != 0;
            case TypeCode.String:
                return (value, client) => value.CreateCodedInput ().ReadString ();
            default:
                break;
            }
            var info = TypeInfo.For (type);
            switch (info.Kind) {
            case TypeKind.Bytes:
                return (value, client) => value.CreateCodedInput ().ReadBytes ().ToByteArray ();
            case TypeKind.Class:
                return (value, client) => {
                    if (client == null)
                        throw new ArgumentException ("Client not passed when decoding remote object");
                    return info.NewObject (client, ReadVarint (value));
                };
            case TypeKind.Struct:
            case TypeKind.Tuple:
                return (value, client) => DecodeTuple (value, info, client);
            case TypeKind.List:
                return (value, client) => DecodeList (value, info, client);
            case TypeKind.Set:
                return (value, client) => DecodeSet (value, info, client);
            case TypeKind.Dictionary:
                return (value, client) => DecodeDictionary (value, info, client);
            case TypeKind.Message:
                return (value, client) => {
                    var message = (IMessage)Activator.CreateInstance (type);
                    message.MergeFrom (value);
                    return message;
                };
            case TypeKind.Event:
                return (value, client) => {
                    var message = new Schema.KRPC.Event ();
                    message.MergeFrom (value);
                    return new Event ((Connection)client, message);
                };
            default:
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        /// <remarks>
        /// A structure carries the values of its fields in order, which is the same encoding as
        /// a tuple of those values. Fields are only ever appended to a structure, so a value
        /// from a newer server may carry more items than the type names, and the extra ones are
        /// ignored. Fewer items than the type names is an error, as the missing ones cannot be
        /// given a value.
        /// </remarks>
        static object DecodeTuple (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (data);
            var values = new object [info.Arguments.Length];
            if (encodedTuple.Items.Count < values.Length)
                throw new ArgumentException (
                    "Value has " + encodedTuple.Items.Count + " items; expected at least " +
                    values.Length);
            for (int i = 0; i < values.Length; i++)
                values [i] = Decode (encodedTuple.Items [i], info.Arguments [i], client);
            return info.NewTuple (values);
        }

        static object DecodeList (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (data);
            var decodeItem = DecoderFor (info.Arguments [0]);
            var list = (IList)info.New ();
            foreach (var item in encodedList.Items)
                list.Add (decodeItem (item, client));
            return list;
        }

        static object DecodeSet (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (data);
            var decodeItem = DecoderFor (info.Arguments [0]);
            var set = info.New ();
            foreach (var item in encodedSet.Items)
                info.Add (set, decodeItem (item, client));
            return set;
        }

        static object DecodeDictionary (ByteString data, TypeInfo info, IConnection client)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (data);
            var decodeKey = DecoderFor (info.Arguments [0]);
            var decodeValue = DecoderFor (info.Arguments [1]);
            var dictionary = (IDictionary)info.New ();
            foreach (var entry in encodedDictionary.Entries) {
                var key = decodeKey (entry.Key, client);
                var value = decodeValue (entry.Value, client);
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
        // Reaching a per-thread static costs several times what making a buffer this small
        // costs, and it is reached once for every value written.

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
