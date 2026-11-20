using System;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.Server
{
    /// <summary>
    /// The origins whose web pages may connect to a server. The setting is a comma-separated
    /// list of origins, <c>localhost</c> for a page served from a loopback address on any port, or
    /// <c>*</c> for any origin.
    /// </summary>
    public sealed class AllowedOrigins
    {
        readonly HashSet<string> origins;

        /// <summary>
        /// Parse the setting. Throws an <see cref="ArgumentException"/> naming each entry that
        /// is not an origin.
        /// </summary>
        public AllowedOrigins (string setting)
        {
            origins = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
            var invalid = new List<string> ();
            foreach (var entry in Entries (setting)) {
                var origin = Canonical (entry);
                if (entry == "*")
                    Any = true;
                else if (entry == "localhost")
                    Local = true;
                else if (origin != null)
                    origins.Add (origin);
                else
                    invalid.Add (entry);
            }
            if (invalid.Count > 0)
                throw new ArgumentException ("Not an origin: " + string.Join (", ", invalid), nameof (setting));
        }

        /// <summary>
        /// Whether every origin is allowed.
        /// </summary>
        public bool Any { get; private set; }

        /// <summary>
        /// Whether a page served from a loopback address is allowed, on any port.
        /// </summary>
        public bool Local { get; private set; }

        /// <summary>
        /// Whether the origins allow no page from another origin.
        /// </summary>
        public bool None {
            get { return !Any && !Local && origins.Count == 0; }
        }

        /// <summary>
        /// Whether the setting is valid: each entry is <c>*</c> or an origin.
        /// </summary>
        public static bool IsValid (string setting)
        {
            return Entries (setting).All (x => x == "*" || x == "localhost" || Canonical (x) != null);
        }

        static IEnumerable<string> Entries (string setting)
        {
            return (setting ?? string.Empty)
                .Split (new [] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select (x => x.Trim ());
        }

        /// <summary>
        /// An origin as a browser writes it in an Origin header: the scheme and host in lower
        /// case, and the port only when it is not the scheme's default. Null if the text is not
        /// an origin, which is a scheme, a host and an optional port, with no path.
        /// </summary>
        static string Canonical (string text)
        {
            Uri uri;
            if (!Uri.TryCreate (text, UriKind.Absolute, out uri))
                return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return null;
            if (uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 ||
                uri.AbsolutePath != "/" || text.EndsWith ("/", StringComparison.Ordinal))
                return null;
            return uri.GetLeftPart (UriPartial.Authority).ToLowerInvariant ();
        }

        /// <summary>
        /// Whether a page with the given origin may call the server. A page opened from a file,
        /// or in a sandboxed frame, has the origin <c>null</c>. Any site can create such a
        /// frame, so only <c>*</c> allows it.
        /// </summary>
        public bool Allows (string origin)
        {
            if (Any)
                return true;
            if (origin == "null")
                return false;
            if (Local && IsLocal (origin))
                return true;
            return origins.Contains (origin.ToLowerInvariant ());
        }

        // An origin whose host is this machine, by name or loopback address
        static bool IsLocal (string origin)
        {
            var canonical = Canonical (origin);
            if (canonical == null)
                return false;
            var host = new Uri (canonical).Host;
            return host == "localhost" || host == "127.0.0.1" || host == "[::1]";
        }

        /// <summary>
        /// The setting, with its entries joined by ", ".
        /// </summary>
        public override string ToString ()
        {
            var entries = origins.OrderBy (x => x, StringComparer.Ordinal).ToList ();
            if (Local)
                entries.Insert (0, "localhost");
            if (Any)
                entries.Insert (0, "*");
            return string.Join (", ", entries);
        }
    }
}
