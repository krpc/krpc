namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamResponse : IMessage
    {
        public uint Id { get; private set; }

        public Response Response { get; set; }

        public StreamResponse (uint id)
        {
            Id = id;
        }
    }
}
