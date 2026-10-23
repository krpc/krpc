using System.Text;

namespace KRPC.Server
{
    /// <summary>
    /// The name of a client in log lines.
    /// </summary>
    public static class ClientLabel
    {
        /// <summary>
        /// The client's address followed by the first 4 bytes of its identifier, in hex, as
        /// in "127.0.0.1 (a1b2c3d4)".
        /// </summary>
        public static string Label (this IClient client)
        {
            var bytes = client.Guid.ToByteArray ();
            var result = new StringBuilder (client.Address);
            result.Append (" (");
            // The bytes in wire order, which Guid.ToString does not use
            for (int i = 0; i < 4; i++)
                result.Append (bytes [i].ToString ("x2", System.Globalization.CultureInfo.InvariantCulture));
            result.Append (')');
            return result.ToString ();
        }
    }
}
