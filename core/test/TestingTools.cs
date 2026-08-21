using System;
using System.IO;
using System.Linq;
using Google.Protobuf;

namespace KRPC.Test
{
    static class TestingTools
    {
        /// <summary>
        /// A directory to put a socket in, short enough for the path of one to fit in a socket
        /// address. The directory a test is given for its temporary files is nested far deeper
        /// than an address has room for, so the platform's own is used directly.
        /// </summary>
        public static string SocketDirectory ()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return "/tmp";
            var local = Environment.GetEnvironmentVariable ("LOCALAPPDATA");
            return string.IsNullOrEmpty (local)
                ? Path.GetTempPath () : Path.Combine (local, "Temp");
        }

        public static string ToHexString (this byte[] data)
        {
            return BitConverter.ToString (data).Replace ("-", string.Empty).ToLowerInvariant ();
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
