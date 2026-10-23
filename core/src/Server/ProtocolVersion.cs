using System.Globalization;

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
        public const uint Current = 2;

        /// <summary>
        /// Whether the server accepts a client with the given protocol version.
        /// </summary>
        public static bool IsSupported (uint version)
        {
            return version == Current;
        }

        /// <summary>
        /// The message sent to a client with an unsupported protocol version.
        /// </summary>
        public static string UnsupportedMessage (uint version)
        {
            if (version == 0)
                return "The client sent no protocol version. The server supports protocol version " +
                    Current.ToString (CultureInfo.InvariantCulture) + ".";
            return "Protocol version " + version.ToString (CultureInfo.InvariantCulture) +
                " is not supported. The server supports protocol version " +
                Current.ToString (CultureInfo.InvariantCulture) + ".";
        }
    }
}
