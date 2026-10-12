using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// An operation did not complete in the time allotted to it.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.TimeoutException))]
    public sealed class TimeoutException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public TimeoutException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public TimeoutException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public TimeoutException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
