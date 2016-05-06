using KRPC.Server;

namespace KRPC
{
    class MalformedHTTPRequestException: ServerException
    {
        public MalformedHTTPRequestException (string message) : base (message)
        {
        }
    }
}
