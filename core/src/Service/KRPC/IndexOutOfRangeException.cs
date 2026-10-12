using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// An index was outside the bounds of a collection, string or array.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.IndexOutOfRangeException))]
    public sealed class IndexOutOfRangeException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public IndexOutOfRangeException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public IndexOutOfRangeException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public IndexOutOfRangeException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
