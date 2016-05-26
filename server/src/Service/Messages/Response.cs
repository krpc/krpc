namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Response : IMessage
    {
        public double Time;
        public bool HasError;
        public string Error = "";
        public bool HasReturnValue;
        public object ReturnValue;
    }
}
