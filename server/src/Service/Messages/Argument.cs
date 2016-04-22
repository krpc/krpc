namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Argument : IMessage
    {
        public uint Position;
        public object Value;
    }
}
