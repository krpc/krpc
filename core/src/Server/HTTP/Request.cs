using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// An HTTP request
    /// </summary>
    public sealed class Request
    {
        #pragma warning disable 1591
        const string NEWLINE = "\r\n";

        public string Method { get; private set; }

        public Uri URI { get; private set; }

        public string Protocol { get; private set; }

        public IDictionary<string, IList<string>> Headers { get; private set; }

        public static Request FromBytes (byte[] data, int index, int count)
        {
            return FromString (Encoding.ASCII.GetString (data, index, count));
        }

        /// <summary>
        /// Get the value of a query string parameter, or null if it is not present. Handles the
        /// parameter appearing in any position, and percent-decodes the value.
        /// </summary>
        public string QueryParameter (string key)
        {
            var query = URI.Query;
            if (query.StartsWith ("?", StringComparison.Ordinal))
                query = query.Substring (1);
            foreach (var pair in query.Split ('&')) {
                if (pair.Length == 0)
                    continue;
                var i = pair.IndexOf ('=');
                var name = i == -1 ? pair : pair.Substring (0, i);
                if (Uri.UnescapeDataString (name) == key)
                    return i == -1 ? string.Empty : Uri.UnescapeDataString (pair.Substring (i + 1));
            }
            return null;
        }

        public static Request FromString (string content)
        {
            var request = new Request ();
            using (var line = content.Split (new [] { NEWLINE }, StringSplitOptions.None).AsEnumerable ().GetEnumerator ()) {
                // Parse request line
                if (!line.MoveNext ())
                    throw new MalformedRequestException ("No request line");
                var requestLineParts = line.Current.Split (' ');
                if (requestLineParts.Length != 3)
                    throw new MalformedRequestException ("Request line malformed");
                if (requestLineParts [0].Length == 0)
                    throw new MalformedRequestException ("Invalid or unsupported method");
                request.Method = requestLineParts [0].ToLowerInvariant ();
                try {
                    var baseUri = new Uri ("http://localhost/");
                    request.URI = new Uri (baseUri, requestLineParts [1]);
                } catch (UriFormatException) {
                    throw new MalformedRequestException ("URI is malformed");
                }
                if (requestLineParts [2].Length == 0)
                    throw new MalformedRequestException ("Invalid or unsupported protocol");
                request.Protocol = requestLineParts [2].ToLowerInvariant ();

                // Parse header fields
                if (!line.MoveNext ())
                    throw new MalformedRequestException ("Request ended early");
                request.Headers = new Dictionary<string, IList<string>>();
                while (line.Current.Length > 0) {
                    var i = line.Current.IndexOf (':');
                    if (i == -1)
                        throw new MalformedRequestException ("Header field malformed");
                    var key = line.Current.Substring (0, i).Trim ().ToLowerInvariant ();
                    var value = line.Current.Substring (i + 1).Trim ();
                    if (key.Length == 0)
                        throw new MalformedRequestException ("Header field key empty");
                    if (value.Length == 0)
                        throw new MalformedRequestException ("Header field value empty");
                    if (request.Headers.ContainsKey (key))
                        throw new MalformedRequestException ("Header field repeated");
                    request.Headers[key] = value.Split (',').Select (x => x.Trim ()).ToList ();
                    if (!line.MoveNext ())
                        throw new MalformedRequestException ("Request ended early");
                }

                // End of request
                if (line.Current.Length > 0 || !line.MoveNext () || line.Current.Length > 0)
                    throw new MalformedRequestException ("Request ended early: ");
                if (line.MoveNext ())
                    throw new MalformedRequestException ("Request too long");
            }

            return request;
        }

        Request ()
        {
        }
    }
}
