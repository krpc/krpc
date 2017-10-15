using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace KRPC.Server.HTTP
{
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    sealed class Request
    {
        const string NEWLINE = "\r\n";

        public string Method { get; private set; }

        public Uri URI { get; private set; }

        public string Protocol { get; private set; }

        public IDictionary<string, IList<string>> Headers { get; private set; }

        public static Request FromBytes (byte[] data, int index, int count)
        {
            return FromString (Encoding.ASCII.GetString (data, index, count));
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
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
                request.Method = requestLineParts [0].ToLower ();
                try {
                    var baseUri = new Uri ("http://localhost/");
                    request.URI = new Uri (baseUri, requestLineParts [1]);
                } catch (UriFormatException) {
                    throw new MalformedRequestException ("URI is malformed");
                }
                if (requestLineParts [2].Length == 0)
                    throw new MalformedRequestException ("Invalid or unsupported protocol");
                request.Protocol = requestLineParts [2].ToLower ();

                // Parse header fields
                if (!line.MoveNext ())
                    throw new MalformedRequestException ("Request ended early");
                request.Headers = new Dictionary<string, IList<string>>();
                while (line.Current.Length > 0) {
                    var i = line.Current.IndexOf (':');
                    if (i == -1)
                        throw new MalformedRequestException ("Header field malformed");
                    var key = line.Current.Substring (0, i).Trim ().ToLower ();
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
