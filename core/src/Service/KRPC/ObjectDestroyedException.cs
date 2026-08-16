using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The game object that a service object refers to no longer exists,
    /// so the object cannot be used. For example, the part it represents
    /// was destroyed, or the vessel it belongs to was recovered.
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
