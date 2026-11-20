using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// Reading and writing JSON. A parsed value is null, a bool, a string, a
    /// <see cref="JsonNumber"/>, a list of values, or a dictionary of values by key.
    /// </summary>
    static class Json
    {
        const int MaxDepth = 64;

        /// <summary>
        /// Parse a JSON document holding one value.
        /// </summary>
        public static object Parse (string text)
        {
            var reader = new Reader (text);
            var value = reader.Value (0);
            reader.SkipWhitespace ();
            if (!reader.AtEnd)
                throw reader.Error ("unexpected text after the value");
            return value;
        }

        /// <summary>
        /// Write a value as JSON. An object is a sequence of key and value pairs, written in
        /// the order of the sequence.
        /// </summary>
        public static string Write (object value)
        {
            var builder = new StringBuilder ();
            Write (builder, value);
            return builder.ToString ();
        }

        static void Write (StringBuilder builder, object value)
        {
            if (value == null) {
                builder.Append ("null");
            } else if (value is bool) {
                builder.Append ((bool)value ? "true" : "false");
            } else if (value is string) {
                WriteString (builder, (string)value);
            } else if (value is JsonNumber) {
                builder.Append (((JsonNumber)value).Text);
            } else if (value is double) {
                WriteDouble (builder, (double)value, ((double)value).ToString ("R", CultureInfo.InvariantCulture));
            } else if (value is float) {
                WriteDouble (builder, (float)value, ((float)value).ToString ("R", CultureInfo.InvariantCulture));
            } else if (value is int || value is long || value is uint || value is ulong) {
                builder.Append (Convert.ToString (value, CultureInfo.InvariantCulture));
            } else if (value is IEnumerable<KeyValuePair<string, object>>) {
                builder.Append ('{');
                var first = true;
                foreach (var entry in (IEnumerable<KeyValuePair<string, object>>)value) {
                    if (!first)
                        builder.Append (", ");
                    first = false;
                    WriteString (builder, entry.Key);
                    builder.Append (": ");
                    Write (builder, entry.Value);
                }
                builder.Append ('}');
            } else if (value is IEnumerable<object>) {
                builder.Append ('[');
                var first = true;
                foreach (var item in (IEnumerable<object>)value) {
                    if (!first)
                        builder.Append (", ");
                    first = false;
                    Write (builder, item);
                }
                builder.Append (']');
            } else {
                throw new ArgumentException ("No JSON form for " + value.GetType ());
            }
        }

        // JSON has no number for NaN or an infinity, so these are written as strings
        static void WriteDouble (StringBuilder builder, double value, string text)
        {
            if (double.IsNaN (value))
                builder.Append ("\"NaN\"");
            else if (double.IsPositiveInfinity (value))
                builder.Append ("\"Infinity\"");
            else if (double.IsNegativeInfinity (value))
                builder.Append ("\"-Infinity\"");
            else
                builder.Append (text);
        }

        static void WriteString (StringBuilder builder, string value)
        {
            builder.Append ('"');
            foreach (var c in value) {
                switch (c) {
                case '"':
                    builder.Append ("\\\"");
                    break;
                case '\\':
                    builder.Append ("\\\\");
                    break;
                case '\n':
                    builder.Append ("\\n");
                    break;
                case '\r':
                    builder.Append ("\\r");
                    break;
                case '\t':
                    builder.Append ("\\t");
                    break;
                default:
                    if (c < 0x20)
                        builder.Append ("\\u").Append (((int)c).ToString ("x4", CultureInfo.InvariantCulture));
                    else
                        builder.Append (c);
                    break;
                }
            }
            builder.Append ('"');
        }

        sealed class Reader
        {
            readonly string text;
            int position;

            public Reader (string value)
            {
                text = value;
            }

            public bool AtEnd {
                get { return position >= text.Length; }
            }

            public JsonException Error (string message)
            {
                return new JsonException (message + " at offset " + position.ToString (CultureInfo.InvariantCulture));
            }

            public void SkipWhitespace ()
            {
                while (!AtEnd) {
                    var c = text [position];
                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
                        break;
                    position++;
                }
            }

            char Next ()
            {
                if (AtEnd)
                    throw Error ("unexpected end of the document");
                return text [position++];
            }

            void Expect (string word)
            {
                if (string.CompareOrdinal (text, position, word, 0, word.Length) != 0)
                    throw Error ("invalid value");
                position += word.Length;
            }

            public object Value (int depth)
            {
                if (depth > MaxDepth)
                    throw Error ("values nested too deeply");
                SkipWhitespace ();
                if (AtEnd)
                    throw Error ("unexpected end of the document");
                var c = text [position];
                switch (c) {
                case '{':
                    return Object (depth);
                case '[':
                    return Array (depth);
                case '"':
                    return String ();
                case 't':
                    Expect ("true");
                    return true;
                case 'f':
                    Expect ("false");
                    return false;
                case 'n':
                    Expect ("null");
                    return null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                        return Number ();
                    throw Error ("invalid value");
                }
            }

            Dictionary<string, object> Object (int depth)
            {
                var result = new Dictionary<string, object> ();
                position++;
                SkipWhitespace ();
                if (!AtEnd && text [position] == '}') {
                    position++;
                    return result;
                }
                while (true) {
                    SkipWhitespace ();
                    if (AtEnd || text [position] != '"')
                        throw Error ("expected a key");
                    var key = String ();
                    if (result.ContainsKey (key))
                        throw Error ("repeated key \"" + key + "\"");
                    SkipWhitespace ();
                    if (Next () != ':')
                        throw Error ("expected ':'");
                    result [key] = Value (depth + 1);
                    SkipWhitespace ();
                    var c = Next ();
                    if (c == '}')
                        return result;
                    if (c != ',')
                        throw Error ("expected ',' or '}'");
                }
            }

            List<object> Array (int depth)
            {
                var result = new List<object> ();
                position++;
                SkipWhitespace ();
                if (!AtEnd && text [position] == ']') {
                    position++;
                    return result;
                }
                while (true) {
                    result.Add (Value (depth + 1));
                    SkipWhitespace ();
                    var c = Next ();
                    if (c == ']')
                        return result;
                    if (c != ',')
                        throw Error ("expected ',' or ']'");
                }
            }

            string String ()
            {
                var builder = new StringBuilder ();
                position++;
                while (true) {
                    var c = Next ();
                    if (c == '"') {
                        var result = builder.ToString ();
                        if (HasLoneSurrogate (result))
                            throw Error ("unpaired surrogate in a string");
                        return result;
                    }
                    if (c < 0x20)
                        throw Error ("control character in a string");
                    if (c != '\\') {
                        builder.Append (c);
                        continue;
                    }
                    c = Next ();
                    switch (c) {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append (c);
                        break;
                    case 'b':
                        builder.Append ('\b');
                        break;
                    case 'f':
                        builder.Append ('\f');
                        break;
                    case 'n':
                        builder.Append ('\n');
                        break;
                    case 'r':
                        builder.Append ('\r');
                        break;
                    case 't':
                        builder.Append ('\t');
                        break;
                    case 'u':
                        if (position + 4 > text.Length)
                            throw Error ("unexpected end of the document");
                        int code;
                        if (!int.TryParse (text.Substring (position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code))
                            throw Error ("invalid escape");
                        builder.Append ((char)code);
                        position += 4;
                        break;
                    default:
                        throw Error ("invalid escape");
                    }
                }
            }

            // A \u escape can give half of a surrogate pair, which has no UTF-8 encoding
            static bool HasLoneSurrogate (string value)
            {
                for (int i = 0; i < value.Length; i++) {
                    if (char.IsHighSurrogate (value [i]) && i + 1 < value.Length && char.IsLowSurrogate (value [i + 1]))
                        i++;
                    else if (char.IsSurrogate (value [i]))
                        return true;
                }
                return false;
            }

            JsonNumber Number ()
            {
                var start = position;
                if (text [position] == '-')
                    position++;
                if (AtEnd)
                    throw Error ("invalid number");
                if (text [position] == '0')
                    position++;
                else if (!Digits ())
                    throw Error ("invalid number");
                if (!AtEnd && text [position] == '.') {
                    position++;
                    if (!Digits ())
                        throw Error ("invalid number");
                }
                if (!AtEnd && (text [position] == 'e' || text [position] == 'E')) {
                    position++;
                    if (!AtEnd && (text [position] == '+' || text [position] == '-'))
                        position++;
                    if (!Digits ())
                        throw Error ("invalid number");
                }
                return new JsonNumber (text.Substring (start, position - start));
            }

            bool Digits ()
            {
                var start = position;
                while (!AtEnd && text [position] >= '0' && text [position] <= '9')
                    position++;
                return position > start;
            }
        }
    }
}
