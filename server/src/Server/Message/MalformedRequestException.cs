using KRPC.Server;

namespace KRPC
{
    class MalformedRequestException: ServerException
    {
        public MalformedRequestException (string message) : base (message)
        {
        }
    }
}
