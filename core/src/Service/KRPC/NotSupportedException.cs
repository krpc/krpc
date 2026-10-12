using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A method was invoked that is not supported.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.NotSupportedException))]
    public sealed class NotSupportedException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NotSupportedException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NotSupportedException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NotSupportedException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
