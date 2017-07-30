namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Event : IMessage
    {
        public Stream Stream { get; private set; }

        public Event (Stream stream)
        {
            Stream = stream;
        }
    }
}
