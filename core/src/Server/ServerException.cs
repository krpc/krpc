using System;

namespace KRPC.Server
{
    /// <summary>
    /// Base class for server exceptions.
    /// </summary>
    public class ServerException : Exception
    {
        #pragma warning disable 1591
        public ServerException ()
        {
        }

        public ServerException (string message) : base (message)
        {
        }

        public ServerException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
