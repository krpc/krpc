using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// An arithmetic or conversion operation overflowed.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.OverflowException))]
    public sealed class OverflowException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public OverflowException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public OverflowException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public OverflowException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
