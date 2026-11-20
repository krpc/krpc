using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf;
using KRPC.Utils;
using Type = KRPC.Schema.KRPC.Type;
using TypeCode = KRPC.Schema.KRPC.Type.Types.TypeCode;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// Converts between a JSON value and the protocol buffers encoding of a value, by the type
    /// the service definitions give it.
    /// </summary>
    sealed class Codec
    {
        readonly Dictionary<string, Schema.KRPC.Enumeration> enumerations = new Dictionary<string, Schema.KRPC.Enumeration> ();
        readonly Dictionary<string, Schema.KRPC.Struct> structs = new Dictionary<string, Schema.KRPC.Struct> ();
        // A buffer per nesting depth, as the items of a value are encoded while it is. A
        // CodedOutputStream allocates 4 KiB, so we reuse them.
        readonly List<Buffer> buffers = new List<Buffer> ();
        int depth;

        sealed class Buffer
        {
            internal readonly MemoryStream Memory = new MemoryStream ();
            internal readonly CodedOutputStream Stream;

            internal Buffer ()
            {
                Stream = new CodedOutputStream (Memory);
            }
        }

        static readonly ByteString absent = ByteString.CopyFrom (new byte [] { 0 });

        public Codec (Schema.KRPC.Services services)
        {
            foreach (var service in services.Services_) {
                foreach (var enumeration in service.Enumerations)
                    enumerations [service.Name + "." + enumeration.Name] = enumeration;
                foreach (var structure in service.Structs)
                    structs [service.Name + "." + structure.Name] = structure;
            }
        }

        /// <summary>
        /// Whether a value of the type can be carried in JSON. An expression or a type of one
        /// cannot, as nothing runs a server side function over HTTP.
        /// </summary>
        public static bool Supported (Type type)
        {
            switch (type.Code) {
            case TypeCode.None:
            case TypeCode.Event:
            case TypeCode.ProcedureCall:
            case TypeCode.Stream:
            case TypeCode.Services:
                return false;
            case TypeCode.Class:
                return type.Service != "KRPC" || (type.Name != "Expression" && type.Name != "Type");
            default:
                return type.Types_.All (Supported);
            }
        }

        /// <summary>
        /// The name of a type, for an error message.
        /// </summary>
        public static string Name (Type type)
        {
            string name;
            switch (type.Code) {
            case TypeCode.Double:
                name = "double";
                break;
            case TypeCode.Float:
                name = "float";
                break;
            case TypeCode.Sint32:
                name = "int32";
                break;
            case TypeCode.Sint64:
                name = "int64";
                break;
            case TypeCode.Uint32:
                name = "uint32";
                break;
            case TypeCode.Uint64:
                name = "uint64";
                break;
            case TypeCode.Bool:
                name = "bool";
                break;
            case TypeCode.String:
                name = "string";
                break;
            case TypeCode.Bytes:
                name = "bytes";
                break;
            case TypeCode.Class:
            case TypeCode.Enumeration:
            case TypeCode.Struct:
                name = type.Service + "." + type.Name;
                break;
            default:
                name = type.Code.ToString ().ToLowerInvariant () +
                    "<" + string.Join (", ", type.Types_.Select (Name)) + ">";
                break;
            }
            return type.Nullable ? name + "?" : name;
        }

        /// <summary>
        /// Encode a JSON value, which is not null, as a value of the type.
        /// <paramref name="what"/> names the value in an error.
        /// </summary>
        public ByteString Encode (object value, Type type, string what)
        {
            return Encode (value, type, what, false);
        }

        // With present, the value follows the bool of a nullable value that is present
        ByteString Encode (object value, Type type, string what, bool present)
        {
            if (depth == buffers.Count)
                buffers.Add (new Buffer ());
            var buffer = buffers [depth];
            buffer.Memory.SetLength (0);
            depth++;
            try {
                if (present)
                    buffer.Stream.WriteBool (true);
                Write (buffer.Stream, value, type, what);
                buffer.Stream.Flush ();
            } catch {
                // The stream may hold the bytes of the failed value, unflushed
                buffers [depth - 1] = new Buffer ();
                throw;
            } finally {
                depth--;
            }
            return ByteString.CopyFrom (buffer.Memory.GetBuffer (), 0, (int)buffer.Memory.Length);
        }

        /// <summary>
        /// Encode an object identifier, as a class value.
        /// </summary>
        public static ByteString EncodeObject (ulong id)
        {
            var bytes = new byte [CodedOutputStream.ComputeUInt64Size (id)];
            var stream = new CodedOutputStream (bytes);
            stream.WriteUInt64 (id);
            stream.Flush ();
            return ByteString.CopyFrom (bytes);
        }

        // A value inside another value. A nullable one starts with a bool saying whether it is present
        ByteString EncodeItem (object value, Type type, string what)
        {
            if (value == null) {
                if (!type.Nullable)
                    throw Invalid (what, "cannot be null");
                return absent;
            }
            return Encode (value, type, what, type.Nullable);
        }

        static RequestException Invalid (string what, string problem)
        {
            return new RequestException (400, what + " " + problem);
        }

        void Write (CodedOutputStream stream, object value, Type type, string what)
        {
            if (value == null)
                throw Invalid (what, "cannot be null");
            switch (type.Code) {
            case TypeCode.Double:
                stream.WriteDouble (ToDouble (value, what));
                break;
            case TypeCode.Float:
                var number = ToDouble (value, what);
                var single = (float)number;
                if (float.IsInfinity (single) && !double.IsInfinity (number))
                    throw Invalid (what, "is out of range for a float");
                stream.WriteFloat (single);
                break;
            case TypeCode.Sint32:
                stream.WriteSInt32 ((int)ToInteger (value, int.MinValue, int.MaxValue, what));
                break;
            case TypeCode.Sint64:
                stream.WriteSInt64 ((long)ToInteger (value, long.MinValue, long.MaxValue, what));
                break;
            case TypeCode.Uint32:
                stream.WriteUInt32 ((uint)ToInteger (value, uint.MinValue, uint.MaxValue, what));
                break;
            case TypeCode.Uint64:
            case TypeCode.Class:
                stream.WriteUInt64 ((ulong)ToInteger (value, ulong.MinValue, ulong.MaxValue, what));
                break;
            case TypeCode.Bool:
                if (!(value is bool))
                    throw Invalid (what, "must be true or false");
                stream.WriteBool ((bool)value);
                break;
            case TypeCode.String:
                stream.WriteString (ToString (value, what));
                break;
            case TypeCode.Bytes:
                try {
                    stream.WriteBytes (ByteString.CopyFrom (Convert.FromBase64String (ToString (value, what))));
                } catch (FormatException) {
                    throw Invalid (what, "must be a base64 string");
                }
                break;
            case TypeCode.Enumeration:
                stream.WriteSInt32 (ToEnumeration (value, type, what));
                break;
            case TypeCode.Struct:
                WriteStruct (stream, value, type, what);
                break;
            case TypeCode.Tuple:
                var tuple = ToList (value, what);
                if (tuple.Count != type.Types_.Count)
                    throw Invalid (what, "must have " + type.Types_.Count + " items");
                var encodedTuple = new Schema.KRPC.Tuple ();
                for (int i = 0; i < tuple.Count; i++)
                    encodedTuple.Items.Add (EncodeItem (tuple [i], type.Types_ [i], what + " item " + i));
                encodedTuple.WriteTo (stream);
                break;
            case TypeCode.List:
                var encodedList = new Schema.KRPC.List ();
                var listElement = what + " element";
                foreach (var item in ToList (value, what))
                    encodedList.Items.Add (EncodeItem (item, type.Types_ [0], listElement));
                encodedList.WriteTo (stream);
                break;
            case TypeCode.Set:
                var encodedSet = new Schema.KRPC.Set ();
                var setElement = what + " element";
                foreach (var item in ToList (value, what))
                    encodedSet.Items.Add (EncodeItem (item, type.Types_ [0], setElement));
                encodedSet.WriteTo (stream);
                break;
            case TypeCode.Dictionary:
                WriteDictionary (stream, value, type, what);
                break;
            default:
                throw Invalid (what, "has a type HTTP cannot carry");
            }
        }

        void WriteStruct (CodedOutputStream stream, object value, Type type, string what)
        {
            var fields = ToObject (value, what);
            var definition = structs [type.Service + "." + type.Name];
            var names = definition.Fields.Select (x => Naming.SnakeCase (x.Name)).ToList ();
            foreach (var key in fields.Keys)
                if (!names.Contains (key))
                    throw Invalid (what, "has no field \"" + key + "\"");
            var encoded = new Schema.KRPC.Tuple ();
            for (int i = 0; i < names.Count; i++) {
                object field;
                if (!fields.TryGetValue (names [i], out field))
                    throw Invalid (what, "is missing field \"" + names [i] + "\"");
                encoded.Items.Add (EncodeItem (field, definition.Fields [i].Type, what + " field " + names [i]));
            }
            encoded.WriteTo (stream);
        }

        // A dictionary with string keys is an object, and any other is a list of key and value pairs
        void WriteDictionary (CodedOutputStream stream, object value, Type type, string what)
        {
            var keyType = type.Types_ [0];
            var valueType = type.Types_ [1];
            var encoded = new Schema.KRPC.Dictionary ();
            var whatKey = what + " key";
            var whatValue = what + " value";
            if (keyType.Code == TypeCode.String) {
                foreach (var entry in ToObject (value, what)) {
                    encoded.Entries.Add (new Schema.KRPC.DictionaryEntry {
                        Key = EncodeItem (entry.Key, keyType, whatKey),
                        Value = EncodeItem (entry.Value, valueType, whatValue)
                    });
                }
            } else {
                foreach (var item in ToList (value, what)) {
                    var pair = item as List<object>;
                    if (pair == null || pair.Count != 2)
                        throw Invalid (what, "must be a list of [key, value] pairs");
                    encoded.Entries.Add (new Schema.KRPC.DictionaryEntry {
                        Key = EncodeItem (pair [0], keyType, whatKey),
                        Value = EncodeItem (pair [1], valueType, whatValue)
                    });
                }
            }
            encoded.WriteTo (stream);
        }

        static string ToString (object value, string what)
        {
            var result = value as string;
            if (result == null)
                throw Invalid (what, "must be a string");
            return result;
        }

        static List<object> ToList (object value, string what)
        {
            var result = value as List<object>;
            if (result == null)
                throw Invalid (what, "must be a list");
            return result;
        }

        static Dictionary<string, object> ToObject (object value, string what)
        {
            var result = value as Dictionary<string, object>;
            if (result == null)
                throw Invalid (what, "must be an object");
            return result;
        }

        // The number's text, as a 64-bit integer may not survive a double
        static decimal ToInteger (object value, decimal minimum, decimal maximum, string what)
        {
            var number = value as JsonNumber;
            var text = number == null ? null : number.Text;
            // Most integers have no fraction or exponent, and need no pattern
            long plain;
            if (text != null && long.TryParse (text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out plain)) {
                if (plain < minimum || plain > maximum)
                    throw Invalid (what, "is out of range");
                return plain;
            }
            var match = text == null ? null : integerPattern.Match (text);
            if (match == null || !match.Success || !IsInteger (match))
                throw Invalid (what, "must be an integer");
            decimal result;
            if (!decimal.TryParse (text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result) ||
                result < minimum || result > maximum)
                throw Invalid (what, "is out of range");
            return result;
        }

        static readonly Regex integerPattern = new Regex (@"^-?([0-9]+)(?:\.([0-9]+))?(?:[eE]([+-]?[0-9]+))?$");

        // Whether a number has no fraction. A decimal rounds a tiny fraction such as 1e-30 to
        // zero, so we check the digits that fall after the point.
        static bool IsInteger (Match match)
        {
            var digits = match.Groups [1].Value + match.Groups [2].Value;
            long exponent = 0;
            if (match.Groups [3].Success && !long.TryParse (match.Groups [3].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
                exponent = match.Groups [3].Value.StartsWith ("-", StringComparison.Ordinal) ? long.MinValue / 2 : long.MaxValue / 2;
            var fractionDigits = match.Groups [2].Value.Length - exponent;
            if (fractionDigits <= 0)
                return true;
            var integerDigits = Math.Max (0, digits.Length - fractionDigits);
            for (var i = (int)integerDigits; i < digits.Length; i++)
                if (digits [i] != '0')
                    return false;
            return true;
        }

        static double ToDouble (object value, string what)
        {
            var number = value as JsonNumber;
            if (number != null) {
                // Mono fails to parse a number beyond the range of a double, where .NET returns infinity
                double result;
                if (!double.TryParse (number.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsInfinity (result))
                    throw Invalid (what, "is out of range for a double");
                return result;
            }
            switch (value as string) {
            case "NaN":
                return double.NaN;
            case "Infinity":
                return double.PositiveInfinity;
            case "-Infinity":
                return double.NegativeInfinity;
            default:
                throw Invalid (what, "must be a number");
            }
        }

        // An enumeration value is its name, and a flags value a list of names
        int ToEnumeration (object value, Type type, string what)
        {
            var definition = enumerations [type.Service + "." + type.Name];
            if (!definition.Flags)
                return EnumerationValue (definition, ToString (value, what), what);
            var result = 0;
            foreach (var name in ToList (value, what))
                result |= EnumerationValue (definition, ToString (name, what), what);
            return result;
        }

        static int EnumerationValue (Schema.KRPC.Enumeration definition, string name, string what)
        {
            foreach (var value in definition.Values)
                if (Naming.SnakeCase (value.Name) == name)
                    return value.Value;
            throw Invalid (what, "must be one of " +
                string.Join (", ", definition.Values.Select (x => Naming.SnakeCase (x.Name))));
        }

        /// <summary>
        /// Decode a value of the type, as JSON.
        /// </summary>
        public object Decode (ByteString value, Type type)
        {
            var stream = new CodedInputStream (value.ToByteArray ());
            switch (type.Code) {
            case TypeCode.Double:
                return stream.ReadDouble ();
            case TypeCode.Float:
                return stream.ReadFloat ();
            case TypeCode.Sint32:
                return stream.ReadSInt32 ();
            case TypeCode.Sint64:
                return stream.ReadSInt64 ();
            case TypeCode.Uint32:
                return stream.ReadUInt32 ();
            case TypeCode.Uint64:
            case TypeCode.Class:
                return stream.ReadUInt64 ();
            case TypeCode.Bool:
                return stream.ReadBool ();
            case TypeCode.String:
                return stream.ReadString ();
            case TypeCode.Bytes:
                return Convert.ToBase64String (stream.ReadBytes ().ToByteArray ());
            case TypeCode.Enumeration:
                return FromEnumeration (stream.ReadSInt32 (), type);
            case TypeCode.Struct:
                var definition = structs [type.Service + "." + type.Name];
                var items = Schema.KRPC.Tuple.Parser.ParseFrom (value).Items;
                var fields = new List<KeyValuePair<string, object>> ();
                for (int i = 0; i < definition.Fields.Count; i++) {
                    var field = definition.Fields [i];
                    fields.Add (new KeyValuePair<string, object> (
                        Naming.SnakeCase (field.Name), DecodeItem (items [i], field.Type)));
                }
                return fields;
            case TypeCode.Tuple:
                var tuple = Schema.KRPC.Tuple.Parser.ParseFrom (value).Items;
                return tuple.Select ((x, i) => DecodeItem (x, type.Types_ [i])).ToList ();
            case TypeCode.List:
                return Schema.KRPC.List.Parser.ParseFrom (value).Items.Select (x => DecodeItem (x, type.Types_ [0])).ToList ();
            case TypeCode.Set:
                return Schema.KRPC.Set.Parser.ParseFrom (value).Items.Select (x => DecodeItem (x, type.Types_ [0])).ToList ();
            case TypeCode.Dictionary:
                var entries = Schema.KRPC.Dictionary.Parser.ParseFrom (value).Entries;
                if (type.Types_ [0].Code == TypeCode.String)
                    return entries.Select (x => new KeyValuePair<string, object> (
                        (string)DecodeItem (x.Key, type.Types_ [0]), DecodeItem (x.Value, type.Types_ [1]))).ToList ();
                return entries.Select (x => (object)new List<object> {
                    DecodeItem (x.Key, type.Types_ [0]), DecodeItem (x.Value, type.Types_ [1])
                }).ToList ();
            default:
                throw new ArgumentException ("No JSON form for " + Name (type));
            }
        }

        object DecodeItem (ByteString value, Type type)
        {
            if (!type.Nullable)
                return Decode (value, type);
            if (value.Length == 0 || value [0] == 0)
                return null;
            var bytes = value.ToByteArray ();
            return Decode (ByteString.CopyFrom (bytes, 1, bytes.Length - 1), type);
        }

        object FromEnumeration (int value, Type type)
        {
            var definition = enumerations [type.Service + "." + type.Name];
            if (!definition.Flags) {
                foreach (var entry in definition.Values)
                    if (entry.Value == value)
                        return Naming.SnakeCase (entry.Name);
                throw NoName (value, type);
            }
            // The single bit values first, then a value of several bits for any bit they leave
            var names = new List<object> ();
            var covered = 0;
            foreach (var entry in definition.Values) {
                var bits = entry.Value;
                if (bits != 0 && (bits & (bits - 1)) == 0 && (value & bits) == bits) {
                    names.Add (Naming.SnakeCase (entry.Name));
                    covered |= bits;
                }
            }
            foreach (var entry in definition.Values) {
                var bits = entry.Value;
                if ((value & ~covered) == 0)
                    break;
                if (bits != 0 && (value & bits) == bits && (bits & ~covered) != 0) {
                    names.Add (Naming.SnakeCase (entry.Name));
                    covered |= bits;
                }
            }
            if (covered != value)
                throw NoName (value, type);
            return names;
        }

        static InvalidOperationException NoName (int value, Type type)
        {
            return new InvalidOperationException (
                type.Service + "." + type.Name + " has no name for " + value.ToString (CultureInfo.InvariantCulture));
        }
    }
}
