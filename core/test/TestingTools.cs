using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Google.Protobuf;

namespace KRPC.Test
{
    static class TestingTools
    {
        [SuppressMessage ("Gendarme.Rules.Globalization", "PreferStringComparisonOverrideRule")]
        public static string ToHexString (this byte[] data)
        {
            return BitConverter.ToString (data).Replace ("-", string.Empty).ToLower ();
        }

        public static string ToHexString (this ByteString data)
        {
            return ToHexString (data.ToByteArray ());
        }

        public static byte[] ToBytes (this string data)
        {
            return Enumerable
                .Range (0, data.Length)
                .Where (x => x % 2 == 0)
                .Select (x => Convert.ToByte (data.Substring (x, 2), 16))
                .ToArray ();
        }

        public static ByteString ToByteString (this string data)
        {
            return ByteString.CopyFrom (data.ToBytes ());
        }
    }
}
