using KRPC.Server;

namespace KRPC
{
    class FramingException : ServerException
    {
        public FramingException (string message) : base (message)
        {
        }
    }
}
