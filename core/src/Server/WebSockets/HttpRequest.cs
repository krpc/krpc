using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KRPC.Server.WebSockets
{
    /// <summary>
    /// An HTTP request
    /// </summary>
    sealed class HttpRequest
    {
        #pragma warning disable 1591
        const string NEWLINE = "\r\n";

        public string Method { get; private set; }

        public Uri URI { get; private set; }

        public string Protocol { get; private set; }

        public IDictionary<string, IList<string>> Headers { get; private set; }

        public static HttpRequest FromBytes (byte[] data, int index, int count)
        {
            return FromString (Encoding.ASCII.GetString (data, index, count));
        }

        public static HttpRequest FromString (string content)
        {
            var request = new HttpRequest ();
            using (var line = content.Split (new [] { NEWLINE }, StringSplitOptions.None).AsEnumerable ().GetEnumerator ()) {
                // Parse request line
                if (!line.MoveNext ())
                    throw new MalformedHttpRequestException ("No request line");
                var requestLineParts = line.Current.Split (' ');
                if (requestLineParts.Length != 3)
                    throw new MalformedHttpRequestException ("Request line malformed");
                if (requestLineParts [0].Length == 0)
                    throw new MalformedHttpRequestException ("Invalid or unsupported method");
                request.Method = requestLineParts [0].ToLowerInvariant ();
                try {
                    var baseUri = new Uri ("http://localhost/");
                    request.URI = new Uri (baseUri, requestLineParts [1]);
                } catch (UriFormatException) {
                    throw new MalformedHttpRequestException ("URI is malformed");
                }
                if (requestLineParts [2].Length == 0)
                    throw new MalformedHttpRequestException ("Invalid or unsupported protocol");
                request.Protocol = requestLineParts [2].ToLowerInvariant ();

                // Parse header fields
                if (!line.MoveNext ())
                    throw new MalformedHttpRequestException ("Request ended early");
                request.Headers = new Dictionary<string, IList<string>>();
                while (line.Current.Length > 0) {
                    var i = line.Current.IndexOf (':');
                    if (i == -1)
                        throw new MalformedHttpRequestException ("Header field malformed");
                    var key = line.Current.Substring (0, i).Trim ().ToLowerInvariant ();
                    var value = line.Current.Substring (i + 1).Trim ();
                    if (key.Length == 0)
                        throw new MalformedHttpRequestException ("Header field key empty");
                    if (value.Length == 0)
                        throw new MalformedHttpRequestException ("Header field value empty");
                    if (request.Headers.ContainsKey (key))
                        throw new MalformedHttpRequestException ("Header field repeated");
                    request.Headers[key] = value.Split (',').Select (x => x.Trim ()).ToList ();
                    if (!line.MoveNext ())
                        throw new MalformedHttpRequestException ("Request ended early");
                }

                // End of request
                if (line.Current.Length > 0 || !line.MoveNext () || line.Current.Length > 0)
                    throw new MalformedHttpRequestException ("Request ended early: ");
                if (line.MoveNext ())
                    throw new MalformedHttpRequestException ("Request too long");
            }

            return request;
        }

        HttpRequest ()
        {
        }
    }
}
