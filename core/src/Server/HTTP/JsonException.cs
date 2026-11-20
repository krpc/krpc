using System;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// Thrown when a JSON document is malformed.
    /// </summary>
    sealed class JsonException : Exception
    {
        public JsonException ()
        {
        }

        public JsonException (string message) : base (message)
        {
        }

        public JsonException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
