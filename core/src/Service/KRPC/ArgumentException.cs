using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A method was invoked where at least one of the passed arguments does not
    /// meet the parameter specification of the method.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.ArgumentException))]
    public sealed class ArgumentException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
