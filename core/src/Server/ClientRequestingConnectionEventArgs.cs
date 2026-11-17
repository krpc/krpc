
namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    public sealed class ClientRequestingConnectionEventArgs : ClientEventArgs
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionEventArgs (IClient client, ClientConnectionRequest request) : base (client)
        {
            Request = request;
        }
    }

    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    public sealed class ClientRequestingConnectionEventArgs<TStream> : ClientEventArgs<TStream>
        where TStream : IStream
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionEventArgs (IClient<TStream> client) : base (client)
        {
            Request = new ClientConnectionRequest ();
        }

        /// <summary>
        /// Convert a generic client requesting connection event to a non-generic one.
        /// </summary>
        public static implicit operator ClientRequestingConnectionEventArgs (ClientRequestingConnectionEventArgs<TStream> args)
        {
            return new ClientRequestingConnectionEventArgs (args.Client, args.Request);
        }
    }
}
