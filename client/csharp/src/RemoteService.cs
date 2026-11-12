namespace KRPC.Client
{
    /// <summary>
    /// Base class for services.
    /// </summary>
    public abstract class RemoteService
    {
        /// <summary>
        /// A connection to the server the service is on.
        /// </summary>
        protected IConnection connection { get; private set; }

        /// <summary>
        /// Construct a service.
        /// </summary>
        protected RemoteService (IConnection serverConnection)
        {
            connection = serverConnection;
        }
    }
}
