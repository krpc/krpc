using System;
using System.Collections;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// Decodes values from the protocol buffer messages of kRPC.
    /// </summary>
    public static class Decoder
    {
        /// <summary>
        /// Decode a value at a slot, of the type the given spec describes. The counterpart of
        /// <see cref="Encoder.Encode"/>.
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
        /// counterpart of <c>Encoder.EncoderFor</c>, and there for the same reason.
        /// </summary>
        static Func<ByteString, IConnection, object> DecoderFor (TypeSpec spec)
        {
            var decode = spec.valueDecoder;
            if (decode == null)
                spec.valueDecoder = decode = BuildDecoder (spec);
            return decode;
        }

        /// <summary>
        /// Build the functions that decode a value of a spec's type, and the values it holds.
        /// </summary>
        /// <remarks>
        /// Throws an exception if a type has no encoding.
        /// </remarks>
        internal static void BuildDecoders (TypeSpec spec)
        {
            BuildDecoders (spec, new System.Collections.Generic.HashSet<TypeSpec> ());
        }

        static void BuildDecoders (TypeSpec spec, System.Collections.Generic.ISet<TypeSpec> built)
        {
            if (!built.Add (spec))
                return;
            ItemDecoderFor (spec);
            var types = TypeInfo.For (spec.Type).Types;
            if (types == null)
                return;
            for (int i = 0; i < types.Length; i++)
                BuildDecoders (spec.At (i), built);
        }

        /// <summary>
        /// A function that decodes one of the values a collection or a structure holds, which
        /// <c>Encoder.ItemEncoderFor</c> wrote.
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
            return (data, client) => {
                if (data.Length == 0)
                    throw new ArgumentException ("A nullable value carries no presence bool");
                // The presence bool is one byte, as a bool value always is
                if (data [0] == 0)
                    return null;
                return decode (ByteString.CopyFrom (data.Span.Slice (1)), client);
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
            // An enumeration decodes to a value of the enumeration, so it unboxes to a Nullable
            // of it as well
            if (type.IsEnum)
                return (value, client) => Enum.ToObject (type, DecodeZigZag32 (ReadVarint (value)));
            switch (Type.GetTypeCode (type)) {
            case TypeCode.Double:
                return (value, client) => BitConverter.Int64BitsToDouble ((long)ReadFixed64 (value));
            case TypeCode.Single:
                return (value, client) => Single32.ToSingle (ReadFixed32 (value));
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
                return (value, client) => new Event ((Connection)client, value.CreateCodedInput ().ReadUInt64 ());
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
