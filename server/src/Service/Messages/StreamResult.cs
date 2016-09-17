namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamResult : IMessage
    {
        public ulong Id { get; private set; }

        public ProcedureResult Result { get; set; }

        public StreamResult (ulong id)
        {
            Id = id;
        }
    }
}
