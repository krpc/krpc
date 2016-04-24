namespace KRPC.Server.HTTP
{
    class MalformedRequest : ServerException
    {
        public MalformedRequest (string message) : base (message)
        {
        }
    }
}
