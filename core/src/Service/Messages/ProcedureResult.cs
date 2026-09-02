namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class ProcedureResult : IMessage
    {
        public bool HasValue { get; private set; }

        /// <summary>
        /// The type of the value, with the nullability of every position inside it. Set where
        /// the call is executed, as that is where the procedure's signature is in hand.
        /// </summary>
        public TypeSpec Spec { get; set; }

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

        public void Reset ()
        {
            value_ = null;
            HasValue = false;
            Spec = null;
            error = null;
            HasError = false;
        }
    }
}
