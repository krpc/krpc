using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A method call or property access was made on a part, or a part module,
    /// whose underlying KSP part no longer exists. This happens when the part has
    /// been destroyed, for example after being removed from the vessel or after the
    /// vessel itself was destroyed.
    /// </summary>
    [KRPCException (Service = "SpaceCenter")]
    public sealed class PartDestroyedException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public PartDestroyedException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public PartDestroyedException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public PartDestroyedException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
