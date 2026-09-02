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
        // The presence bool a null carries at a nullable position, written exactly as a bool
        // value is
        static readonly ByteString absent = ByteString.CopyFrom (new byte [] { 0 });

        /// <summary>
        /// Encode a value at a slot, of the type the given spec describes.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        /// <remarks>
        /// A slot carries its null beside the encoded bytes, in the is_null flag of the message
        /// around it, so a null encodes to nothing and the nullability of the spec is not read
        /// here. A value inside another value carries a presence bool instead.
        /// </remarks>
        public static ByteString Encode (object value, TypeSpec spec)
        {
            if (ReferenceEquals (spec, null))
                throw new ArgumentNullException (nameof (spec));
            if (value == null)
                return null;
            if (!spec.Type.IsInstanceOfType (value))
                throw new ArgumentException (
                    "Value of type " + value.GetType () +
                    " cannot be encoded to type " + spec.Type);
            return EncoderFor (spec) (value);
        }

        /// <summary>
        /// A function that encodes one value of the type the given spec describes.
        /// </summary>
        /// <remarks>
        /// A collection carries many values of the same type, and reaching the code that writes
        /// one of them means asking the type whether it is an enumeration and which type code it
        /// carries, and for anything that is not a number or a string, what kind of value it is.
        /// None of those answers change, so a collection asks once and calls what it gets back
        /// for each of its items.
        /// </remarks>
        static Func<object, ByteString> EncoderFor (TypeSpec spec)
        {
            var encode = spec.valueEncoder;
            if (encode == null)
                spec.valueEncoder = encode = BuildEncoder (spec);
            return encode;
        }

        /// <summary>
        /// A function that encodes one of the values a collection or a structure holds, at the
        /// position the given spec describes. A position that can hold null carries a presence
        /// bool before its value, and holds nothing else when that bool is false.
        /// </summary>
        static Func<object, ByteString> ItemEncoderFor (TypeSpec spec)
        {
            var encode = spec.itemEncoder;
            if (encode == null)
                spec.itemEncoder = encode = BuildItemEncoder (spec);
            return encode;
        }

        /// <remarks>
        /// A value reached this way is an item of a collection whose type says what its items
        /// are, so unlike <see cref="Encode"/> this does not ask each one whether it is of that
        /// type. Asking is a call into the type system, and asking it once per item is what made
        /// writing a collection cost several times what reading one back costs. A null is still
        /// rejected, as a collection of a class type can hold one.
        /// </remarks>
        static Func<object, ByteString> BuildItemEncoder (TypeSpec spec)
        {
            var write = EncoderFor (spec);
            var type = spec.Type;
            if (!spec.Nullable)
                return value => {
                    if (value == null)
                        throw new ArgumentException ("null cannot be encoded to type " + type);
                    return write (value);
                };
            return value => value == null ? absent : WithPresence (write (value));
        }

        static ByteString WithPresence (ByteString encoded)
        {
            var buffer = new byte [encoded.Length + 1];
            buffer [0] = 1;
            encoded.CopyTo (buffer, 1);
            return ByteString.CopyFrom (buffer, 0, buffer.Length);
        }

        /// <summary>
        /// A function that writes one value of the type the given spec describes, without
        /// checking it first.
        /// </summary>
        /// <remarks>
        /// A number, a string or an object identifier is a handful of bytes on the wire, and its
        /// bytes are written into a buffer of that size. A coded output stream carries a buffer
        /// of several kilobytes of its own, which is far more to allocate for one number than
        /// writing the number costs. Anything carried by a protobuf message is left to protobuf
        /// to serialize.
        /// </remarks>
        static Func<object, ByteString> BuildEncoder (TypeSpec spec)
        {
            var type = spec.Type;
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
                return value => EncodeTuple (value, info, spec);
            case TypeKind.List:
                return value => EncodeList (value, spec);
            case TypeKind.Set:
                return value => EncodeSet (value, spec);
            case TypeKind.Dictionary:
                return value => EncodeDictionary (value, spec);
            case TypeKind.Message:
                return value => ((IMessage)value).ToByteString ();
            default:
                throw new ArgumentException (type + " is not a serializable type");
            }
        }

        static ByteString EncodeTuple (object value, TypeInfo info, TypeSpec spec)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            for (int i = 0; i < info.Items.Length; i++)
                encodedTuple.Items.Add (ItemEncoderFor (spec.At (i)) (info.Items [i] (value)));
            return encodedTuple.ToByteString ();
        }

        static ByteString EncodeList (object value, TypeSpec spec)
        {
            var encodedList = new Schema.KRPC.List ();
            var encodeItem = ItemEncoderFor (spec.At (0));
            foreach (var item in (IList)value)
                encodedList.Items.Add (encodeItem (item));
            return encodedList.ToByteString ();
        }

        static ByteString EncodeSet (object value, TypeSpec spec)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var encodeItem = ItemEncoderFor (spec.At (0));
            foreach (var item in (IEnumerable)value)
                encodedSet.Items.Add (encodeItem (item));
            return encodedSet.ToByteString ();
        }

        static ByteString EncodeDictionary (object value, TypeSpec spec)
        {
            var encodeKey = ItemEncoderFor (spec.At (0));
            var encodeValue = ItemEncoderFor (spec.At (1));
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            foreach (DictionaryEntry entry in (IDictionary)value) {
                var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = encodeKey (entry.Key);
                encodedEntry.Value = encodeValue (entry.Value);
                encodedDictionary.Entries.Add (encodedEntry);
            }
            return encodedDictionary.ToByteString ();
        }

        /// <summary>
        /// Decode a value at a slot, of the type the given spec describes. The counterpart of
        /// <see cref="Encode"/>.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static object Decode (ByteString value, TypeSpec spec, IConnection client)
        {
            if (ReferenceEquals (spec, null))
                throw new ArgumentNullException (nameof (spec));
            return DecoderFor (spec) (value, client);
        }

        /// <summary>
        /// A function that decodes one value of the type the given spec describes. The
        /// counterpart of <see cref="EncoderFor"/>, and there for the same reason.
        /// </summary>
        static Func<ByteString, IConnection, object> DecoderFor (TypeSpec spec)
        {
            var decode = spec.valueDecoder;
            if (decode == null)
                spec.valueDecoder = decode = BuildDecoder (spec);
            return decode;
        }

        /// <summary>
        /// A function that decodes one of the values a collection or a structure holds, which
        /// <see cref="ItemEncoderFor"/> wrote.
        /// </summary>
        static Func<ByteString, IConnection, object> ItemDecoderFor (TypeSpec spec)
        {
            var decode = spec.itemDecoder;
            if (decode == null)
                spec.itemDecoder = decode = BuildItemDecoder (spec);
            return decode;
        }

        static Func<ByteString, IConnection, object> BuildItemDecoder (TypeSpec spec)
        {
            var decode = DecoderFor (spec);
            if (!spec.Nullable)
                return decode;
            var type = spec.Type;
            var decodeValue = decode;
            // An enumeration decodes to its integer value, which unboxes to the enumeration
            // itself but not to a Nullable of it
            if (type.IsEnum)
                decodeValue = (data, client) => Enum.ToObject (type, decode (data, client));
            return (data, client) => {
                if (data.Length == 0)
                    throw new ArgumentException ("A nullable value carries no presence bool");
                // The presence bool is one byte, as a bool value always is
                if (data [0] == 0)
                    return null;
                return decodeValue (ByteString.CopyFrom (data.Span.Slice (1)), client);
            };
        }

        /// <summary>
        /// A function that reads one value of the type the given spec describes.
        /// </summary>
        /// <remarks>
        /// The bytes of a number, a string or an object identifier are read where they are, for
        /// the same reason the encoding writes them there: a coded input stream of its own is
        /// more to allocate for one value than reading the value costs.
        /// </remarks>
        static Func<ByteString, IConnection, object> BuildDecoder (TypeSpec spec)
        {
            var type = spec.Type;
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
                return (value, client) => DecodeTuple (value, info, spec, client);
            case TypeKind.List:
                return (value, client) => DecodeList (value, info, spec, client);
            case TypeKind.Set:
                return (value, client) => DecodeSet (value, info, spec, client);
            case TypeKind.Dictionary:
                return (value, client) => DecodeDictionary (value, info, spec, client);
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
        static object DecodeTuple (
            ByteString data, TypeInfo info, TypeSpec spec, IConnection client)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (data);
            var values = new object [info.Items.Length];
            if (encodedTuple.Items.Count < values.Length)
                throw new ArgumentException (
                    "Value has " + encodedTuple.Items.Count + " items; expected at least " +
                    values.Length);
            for (int i = 0; i < values.Length; i++)
                values [i] = ItemDecoderFor (spec.At (i)) (encodedTuple.Items [i], client);
            return info.NewTuple (values);
        }

        static object DecodeList (
            ByteString data, TypeInfo info, TypeSpec spec, IConnection client)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (data);
            var list = (IList)info.New ();
            var decodeItem = ItemDecoderFor (spec.At (0));
            foreach (var item in encodedList.Items)
                list.Add (decodeItem (item, client));
            return list;
        }

        static object DecodeSet (
            ByteString data, TypeInfo info, TypeSpec spec, IConnection client)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (data);
            var decodeItem = ItemDecoderFor (spec.At (0));
            var set = info.New ();
            foreach (var item in encodedSet.Items)
                info.Add (set, decodeItem (item, client));
            return set;
        }

        static object DecodeDictionary (
            ByteString data, TypeInfo info, TypeSpec spec, IConnection client)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (data);
            var decodeKey = ItemDecoderFor (spec.At (0));
            var decodeValue = ItemDecoderFor (spec.At (1));
            var dictionary = (IDictionary)info.New ();
            foreach (var entry in encodedDictionary.Entries)
                dictionary [decodeKey (entry.Key, client)] = decodeValue (entry.Value, client);
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
