using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// An integer was divided by zero.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.DivideByZeroException))]
    public sealed class DivideByZeroException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public DivideByZeroException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public DivideByZeroException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public DivideByZeroException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
