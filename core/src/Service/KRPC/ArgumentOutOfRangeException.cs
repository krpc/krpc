using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The value of an argument is outside the allowable range of values as defined by the invoked method.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.ArgumentOutOfRangeException))]
    public sealed class ArgumentOutOfRangeException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentOutOfRangeException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentOutOfRangeException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ArgumentOutOfRangeException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
