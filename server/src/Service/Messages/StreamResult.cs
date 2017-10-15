namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamResult : IMessage
    {
        public ulong Id { get; set; }

        public ProcedureResult Result { get; set; }

        public StreamResult ()
        {
            Result = new ProcedureResult ();
        }
    }
}
