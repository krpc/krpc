using System;

namespace KRPC.Server.Message
{
    sealed class MalformedRequestException: ServerException
    {
        public MalformedRequestException ()
        {
        }

        public MalformedRequestException (string message) : base (message)
        {
        }

        public MalformedRequestException (string message, Exception innerException) : base (message, innerException)
        {
        }

        public MalformedRequestException (string message, int bytesRead) : base (message)
        {
            BytesRead = bytesRead;
        }

        /// <summary>
        /// The number of bytes the malformed message occupied, which are dropped so that a serial
        /// line reads what follows as a new message.
        /// </summary>
        public int BytesRead { get; private set; }
    }
}
