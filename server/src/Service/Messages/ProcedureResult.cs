namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class ProcedureResult : IMessage
    {
        public bool HasValue { get; private set; }

        public object Value {
            get { return value_; }
            set {
                value_ = value;
                HasValue = true;
            }
        }

        public bool HasError { get; private set; }

        public Error Error {
            get { return error; }
            set {
                error = value;
                HasError = true;
            }
        }

        object value_;

        Error error;
    }
}
