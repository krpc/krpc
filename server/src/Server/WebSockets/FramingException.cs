using KRPC.Server;

namespace KRPC
{
    class FramingException : ServerException
    {
        public FramingException (ushort status, string message) : base (message)
        {
            Status = status;
        }

        public ushort Status { get; private set; }
    }
}
