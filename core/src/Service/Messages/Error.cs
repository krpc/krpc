namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Error : IMessage
    {
        public string Service { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public string StackTrace { get; private set; }

        public Error (string description, string stackTrace)
        {
            Service = string.Empty;
            Name = string.Empty;
            Description = description;
            StackTrace = stackTrace;
        }

        public Error (string service, string name, string description)
        {
            Service = service;
            Name = name;
            Description = description;
            StackTrace = string.Empty;
        }

        public Error (string service, string name, string description, string stackTrace)
        {
            Service = service;
            Name = name;
            Description = description;
            StackTrace = stackTrace;
        }
    }
}
