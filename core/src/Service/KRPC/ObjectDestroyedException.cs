using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A method call or property access was made on an object whose underlying game
    /// object no longer exists. This happens when the object has been destroyed, for
    /// example a part that was removed from its vessel, or whose vessel was destroyed.
    /// </summary>
    [KRPCException (Service = "KRPC")]
    public sealed class ObjectDestroyedException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectDestroyedException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectDestroyedException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectDestroyedException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
