using System.Collections.Generic;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// An HTTP request, as <see cref="Connection"/> parses it.
    /// </summary>
    sealed class HttpRequest
    {
        public HttpRequest (string method, string target, IDictionary<string, string> headers, byte[] body, bool keepAlive)
        {
            Method = method;
            Target = target;
            Headers = headers;
            Body = body;
            KeepAlive = keepAlive;
        }

        public string Method { get; private set; }

        /// <summary>
        /// The request target, the path and any query.
        /// </summary>
        public string Target { get; private set; }

        /// <summary>
        /// The header fields by their name in lower case. A repeated field holds its values
        /// joined by ", ".
        /// </summary>
        public IDictionary<string, string> Headers { get; private set; }

        public byte[] Body { get; private set; }

        /// <summary>
        /// Whether the connection stays open after the response.
        /// </summary>
        public bool KeepAlive { get; private set; }

        public string Header (string name)
        {
            string value;
            return Headers.TryGetValue (name, out value) ? value : null;
        }
    }
}
