
namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Stream : IMessage
    {
        public ulong Id { get; private set; }

        public Stream (ulong id)
        {
            Id = id;
        }
    }
}
