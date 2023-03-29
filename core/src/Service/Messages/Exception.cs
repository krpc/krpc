
namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Exception : IMessage
    {
        public string Name { get; private set; }

        public string Documentation { get; set; }

        public Exception (string name)
        {
            Name = name;
            Documentation = string.Empty;
        }
    }
}
