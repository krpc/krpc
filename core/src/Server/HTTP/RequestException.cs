using System;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// Thrown when an HTTP request cannot be carried out, with the status of the response.
    /// </summary>
    sealed class RequestException : Exception
    {
        public RequestException (int status, string message) : base (message)
        {
            Status = status;
        }

        public int Status { get; private set; }

        /// <summary>
        /// The methods the path takes, for a 405 response.
        /// </summary>
        public string Allow { get; set; }

        public RequestException ()
        {
            Status = 400;
        }

        public RequestException (string message) : base (message)
        {
            Status = 400;
        }

        public RequestException (string message, Exception innerException) : base (message, innerException)
        {
            Status = 400;
        }
    }
}
