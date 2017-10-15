namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Class : IMessage
    {
        public string Name { get; private set; }

        public string Documentation { get; set; }

        public Class (string name)
        {
            Name = name;
            Documentation = string.Empty;
        }
    }
}
