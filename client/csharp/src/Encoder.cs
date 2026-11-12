using System;
using System.Collections;
using System.Text;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// Encodes values for the protocol buffer messages of kRPC.
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
                return value => Fixed32 (Single32.ToBits ((float)value));
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
                return value => Varint (RemoteObject.GetObjectId ((RemoteObject)value));
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

        static ulong EncodeZigZag (int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        static ulong EncodeZigZag (long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
    }
}
