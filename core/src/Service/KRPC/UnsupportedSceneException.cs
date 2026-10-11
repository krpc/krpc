using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// The procedure is not available in the current game scene.
    /// </summary>
    [KRPCException (Service = "KRPC")]
    public sealed class UnsupportedSceneException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public UnsupportedSceneException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public UnsupportedSceneException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public UnsupportedSceneException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
