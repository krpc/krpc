using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KRPC.Server.HTTP
{
    class HTTPRequest
    {
        const string NEWLINE = "\r\n";

        public string Method { get; private set; }

        public Uri URI { get; private set; }

        public string Protocol { get; private set; }

        public IDictionary<string,string> Headers { get; private set; }

        public static HTTPRequest FromBytes (byte[] data, int index, int count)
        {
            return FromString (Encoding.ASCII.GetString (data, index, count));
        }

        public static HTTPRequest FromString (string content)
        {
            var request = new HTTPRequest ();
            var line = content.Split (new [] { NEWLINE }, StringSplitOptions.None).AsEnumerable ().GetEnumerator ();

            // Parse request line
            if (!line.MoveNext ())
                throw new MalformedHTTPRequestException ("No request line");
            var requestLineParts = line.Current.Split (' ');
            if (requestLineParts.Length != 3)
                throw new MalformedHTTPRequestException ("Request line malformed");
            if (requestLineParts [0] == "")
                throw new MalformedHTTPRequestException ("Invalid or unsupported method");
            request.Method = requestLineParts [0];
            try {
                var baseUri = new Uri ("http://localhost/");
                request.URI = new Uri (baseUri, requestLineParts [1]);
            } catch (UriFormatException) {
                throw new MalformedHTTPRequestException ("URI is malformed");
            }
            if (requestLineParts [2] == "")
                throw new MalformedHTTPRequestException ("Invalid or unsupported protocol");
            request.Protocol = requestLineParts [2];

            // Parse header fields
            if (!line.MoveNext ())
                throw new MalformedHTTPRequestException ("Request ended early");
            request.Headers = new Dictionary<string, string> ();
            while (line.Current != "") {
                var i = line.Current.IndexOf (':');
                if (i == -1)
                    throw new MalformedHTTPRequestException ("Header field malformed");
                var key = line.Current.Substring (0, i).Trim ();
                var value = line.Current.Substring (i + 1).Trim ();
                if (key == "")
                    throw new MalformedHTTPRequestException ("Header field key empty");
                if (value == "")
                    throw new MalformedHTTPRequestException ("Header field value empty");
                if (request.Headers.ContainsKey (key))
                    throw new MalformedHTTPRequestException ("Header field repeated");
                request.Headers [key] = value;
                if (!line.MoveNext ())
                    throw new MalformedHTTPRequestException ("Request ended early");
            }

            // End of request
            if (line.Current != "" || !line.MoveNext () || line.Current != "")
                throw new MalformedHTTPRequestException ("Request ended early: ");
            if (line.MoveNext ())
                throw new MalformedHTTPRequestException ("Request too long");

            return request;
        }

        HTTPRequest ()
        {
        }
    }
}

