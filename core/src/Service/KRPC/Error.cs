using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A general error, described by its message.
    /// </summary>
    [KRPCException (Service = "KRPC")]
    public sealed class Error : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public Error ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public Error (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public Error (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
