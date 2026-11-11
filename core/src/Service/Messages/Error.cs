namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Error : IMessage
    {
        public string Service { get; private set; }

        public string Exception { get; private set; }

        public uint ServiceId { get; private set; }

        public uint ExceptionId { get; private set; }

        public string Description { get; private set; }

        public string StackTrace { get; private set; }

        public Error (string description, string stackTrace)
        {
            Service = string.Empty;
            Exception = string.Empty;
            Description = description;
            StackTrace = stackTrace;
        }

        public Error (string service, string exception, uint serviceId, uint exceptionId, string description, string stackTrace)
        {
            Service = service;
            Exception = exception;
            ServiceId = serviceId;
            ExceptionId = exceptionId;
            Description = description;
            StackTrace = stackTrace;
        }
    }
}
