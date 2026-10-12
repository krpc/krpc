using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A value was null where its type does not allow one.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.NullReferenceException))]
    public sealed class NullReferenceException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NullReferenceException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NullReferenceException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public NullReferenceException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
