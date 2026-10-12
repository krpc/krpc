using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A key was not found in a dictionary.
    /// </summary>
    [KRPCException (Service = "KRPC", MappedException = typeof (System.Collections.Generic.KeyNotFoundException))]
    public sealed class KeyNotFoundException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public KeyNotFoundException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public KeyNotFoundException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public KeyNotFoundException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
