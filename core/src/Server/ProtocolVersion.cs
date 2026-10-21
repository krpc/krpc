using System.Globalization;
using KRPC.Utils;

namespace KRPC.Server
{
    /// <summary>
    /// The protocol version, exchanged in the connection handshake.
    /// </summary>
    static class ProtocolVersion
    {
        /// <summary>
        /// The protocol version of the server.
        /// </summary>
        public const uint Current = 1;

        /// <summary>
        /// Whether the server accepts a client with the given protocol version. Version 0 is a
        /// client that sent no version, and is served as the current version.
        /// </summary>
        public static bool IsSupported (uint version)
        {
            return version <= Current;
        }

        /// <summary>
        /// The message sent to a client with an unsupported protocol version.
        /// </summary>
        public static string UnsupportedMessage (uint version)
        {
            return "Protocol version " + version.ToString (CultureInfo.InvariantCulture) +
                " is not supported. The server supports protocol versions 0 to " +
                Current.ToString (CultureInfo.InvariantCulture) + ".";
        }

        /// <summary>
        /// Log a warning for a client that sent no protocol version.
        /// </summary>
        public static void WarnIfUnversioned (uint version, string name, string address)
        {
            if (version != 0)
                return;
            Logger.WriteLine (
                "Client '" + name + "' (" + address + ") sent no protocol version, " +
                "and is served as protocol version " + Current.ToString (CultureInfo.InvariantCulture) +
                ". It may be a client from before kRPC 0.7.", Logger.Severity.Warning);
        }
    }
}
