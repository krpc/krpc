namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamResponse : IMessage
    {
        public ulong Id { get; private set; }

        public Response Response { get; set; }

        public StreamResponse (ulong id)
        {
            Id = id;
        }
    }
}
