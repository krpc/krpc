using System;
using System.Text;

namespace KRPC.Server.HTTP
{
    class HTTPResponse
    {
        const string PROTOCOL = "HTTP/1.1";

        public static HTTPResponse BadRequest {
            get { return new HTTPResponse (400, "Bad Request"); }
        }

        public static HTTPResponse Forbidden {
            get { return new HTTPResponse (403, "Forbidden"); }
        }

        public static HTTPResponse NotFound {
            get { return new HTTPResponse (404, "Not Found"); }
        }

        public static HTTPResponse MethodNotAllowed {
            get { return new HTTPResponse (405, "Method Not Allowed"); }
        }

        public static HTTPResponse UpgradeRequired {
            get { return new HTTPResponse (426, "Upgrade Required"); }
        }

        public static HTTPResponse InternalServerError {
            get { return new HTTPResponse (500, "Internal Server Error"); }
        }

        public static HTTPResponse HTTPVersionNotSupported {
            get { return new HTTPResponse (505, "HTTP Version Not Supported"); }
        }

        const string NEWLINE = "\r\n";
        readonly StringBuilder contents = new StringBuilder ();

        public string Body { get; set; }

        public HTTPResponse (uint status, string reason)
        {
            if (reason.Trim () == "" || reason.Contains ("\r") || reason.Contains ("\n"))
                throw new ArgumentException ("Type is malformed");
            contents.Append (PROTOCOL + " " + status + " " + reason + NEWLINE);
            Body = "";
        }

        public void AddHeaderField (string key, string value)
        {
            if (key.Trim () == "" || value.Trim () == "")
                throw new ArgumentException ("Attribute is malformed");
            string field = key + ": " + value;
            if (field.Contains ("\r") || field.Contains ("\n"))
                throw new ArgumentException ("Attribute is malformed");
            contents.Append (field + NEWLINE);
        }

        public override string ToString ()
        {
            var result = contents + NEWLINE;
            if (Body.Trim () != "")
                result += Body.Trim ();
            return result;
        }

        public byte[] ToBytes ()
        {
            return Encoding.ASCII.GetBytes (ToString ());
        }
    }
}

