namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Parameter : IMessage
    {
        public string Name = "";
        public string Type = "";
        public bool HasDefaultValue;
        public object DefaultValue;
    }
}
