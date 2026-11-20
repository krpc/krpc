using System;
using System.Text;

namespace KRPC.Server.WebSockets
{
    sealed class HttpResponse
    {
        const string PROTOCOL = "HTTP/1.1";

        public static HttpResponse CreateBadRequest (string message = null) {
            return new HttpResponse (400, "Bad Request", message);
        }

        public static HttpResponse CreateForbidden (string message = null) {
            return new HttpResponse (403, "Forbidden", message);
        }

        public static HttpResponse CreateNotFound (string message = null) {
            return new HttpResponse (404, "Not Found", message);
        }

        public static HttpResponse CreateMethodNotAllowed (string message = null) {
            return new HttpResponse (405, "Method Not Allowed", message);
        }

        public static HttpResponse CreateUpgradeRequired (string message = null) {
            return new HttpResponse (426, "Upgrade Required", message);
        }

        public static HttpResponse CreateHTTPVersionNotSupported (string message = null) {
            return new HttpResponse (505, "HTTP Version Not Supported", message);
        }

        const string NEWLINE = "\r\n";
        readonly StringBuilder contents = new StringBuilder ();

        public string Body { get; set; }

        public HttpResponse (uint status, string reason, string body = null)
        {
            if (body == null)
                body = string.Empty;
            if (reason.Trim ().Length == 0 || reason.Contains ("\r") || reason.Contains ("\n"))
                throw new ArgumentException ("Type is malformed");
            contents.Append (PROTOCOL + " " + status + " " + reason + NEWLINE);
            Body = body;
        }

        public void AddHeaderField (string key, string value)
        {
            if (key.Trim ().Length == 0 || value.Trim ().Length == 0)
                throw new ArgumentException ("Attribute is malformed");
            string field = key + ": " + value;
            if (field.Contains ("\r") || field.Contains ("\n"))
                throw new ArgumentException ("Attribute is malformed");
            contents.Append (field + NEWLINE);
        }

        public override string ToString ()
        {
            var result = contents + NEWLINE;
            if (Body.Trim ().Length > 0)
                result += Body.Trim ();
            return result;
        }

        public byte[] ToBytes ()
        {
            return Encoding.ASCII.GetBytes (ToString ());
        }
    }
}
