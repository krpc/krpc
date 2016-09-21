using System;

namespace KRPC.Server.WebSockets
{
    sealed class FramingException : ServerException
    {
        public FramingException (ushort status, string message) : base (message)
        {
            Status = status;
        }

        public ushort Status { get; private set; }

        public FramingException ()
        {
        }

        public FramingException (string message) : base (message)
        {
        }

        public FramingException (string message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
