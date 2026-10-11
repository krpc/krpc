using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The game object that a service object refers to exists, but is not loaded.
    /// For example, the game is loading, or the vessel carrying the part is unloaded.
    /// </summary>
    [KRPCException (Service = "KRPC")]
    public sealed class ObjectNotLoadedException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectNotLoadedException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectNotLoadedException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public ObjectNotLoadedException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
