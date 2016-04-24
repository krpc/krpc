using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace KRPC.Server.HTTP
{
    class HTTPRequest
    {
        const string NEWLINE = "\r\n";

        public string Method { get; private set; }

        public string URI { get; private set; }

        public string Protocol { get; private set; }

        public IDictionary<string,string> Headers { get; private set; }

        public static HTTPRequest FromBytes (byte[] data, int index, int count)
        {
            var content = Encoding.ASCII.GetString (data, index, count);
            var request = new HTTPRequest ();
            var line = content.Split (new [] { NEWLINE }, StringSplitOptions.None).AsEnumerable ().GetEnumerator ();

            // Parse request line
            if (!line.MoveNext ())
                throw new MalformedRequest ("No request line");
            var requestLineParts = line.Current.Split (' ');
            if (requestLineParts.Length != 3)
                throw new MalformedRequest ("Request line malformed");
            request.Method = requestLineParts [0];
            request.URI = requestLineParts [1];
            request.Protocol = requestLineParts [2];

            // Parse header fields
            if (!line.MoveNext ())
                throw new MalformedRequest ("Request ended early");
            request.Headers = new Dictionary<string, string> ();
            while (line.Current != "") {
                var i = line.Current.IndexOf (':');
                if (i == -1)
                    throw new MalformedRequest ("Header field malformed");
                var key = line.Current.Substring (0, i).Trim ();
                var value = line.Current.Substring (i + 1).Trim ();
                if (key == "")
                    throw new MalformedRequest ("Header field key empty");
                if (value == "")
                    throw new MalformedRequest ("Header field value empty, for " + key);
                request.Headers [key] = value;
                if (!line.MoveNext ())
                    throw new MalformedRequest ("Request ended early");
            }

            // End of request
            if (line.Current != "")
                throw new MalformedRequest ("Request ended early: ");
            //FIXME: how many blank lines are allowed at the end?
            while (line.Current == "" && line.MoveNext ())
                continue;
            if (line.MoveNext ())
                throw new MalformedRequest ("Request too long");

            return request;
        }

        HTTPRequest ()
        {
        }
    }
}

