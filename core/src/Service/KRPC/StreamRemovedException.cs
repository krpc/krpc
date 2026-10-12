using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A stream was removed by the server, e.g. an event that has finished.
    /// </summary>
    [KRPCException (Service = "KRPC")]
    public sealed class StreamRemovedException : System.Exception
    {
        /// <summary>
        /// Construct the exception.
        /// </summary>
        public StreamRemovedException ()
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public StreamRemovedException (string message) :
            base (message)
        {
        }

        /// <summary>
        /// Construct the exception.
        /// </summary>
        public StreamRemovedException (string message, System.Exception innerException) :
            base (message, innerException)
        {
        }
    }
}
