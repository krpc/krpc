using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace KRPC.Server.HTTP
{
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    sealed class Response
    {
        const string PROTOCOL = "HTTP/1.1";

        public static Response CreateBadRequest (string message = null) {
            return new Response (400, "Bad Request", message);
        }

        public static Response CreateNotFound (string message = null) {
            return new Response (404, "Not Found", message);
        }

        public static Response CreateMethodNotAllowed (string message = null) {
            return new Response (405, "Method Not Allowed", message);
        }

        public static Response CreateUpgradeRequired (string message = null) {
            return new Response (426, "Upgrade Required", message);
        }

        public static Response CreateHTTPVersionNotSupported (string message = null) {
            return new Response (505, "HTTP Version Not Supported", message);
        }

        const string NEWLINE = "\r\n";
        readonly StringBuilder contents = new StringBuilder ();

        public string Body { get; set; }

        public Response (uint status, string reason, string body = null)
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
