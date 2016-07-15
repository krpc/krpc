namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    sealed class ClientRequestingConnectionEventArgs : ClientEventArgs
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        public ClientRequestingConnectionEventArgs (IClient client, ClientConnectionRequest request) : base (client)
        {
            Request = request;
        }
    }

    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    sealed class ClientRequestingConnectionEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        public ClientRequestingConnectionEventArgs (IClient<TIn,TOut> client) : base (client)
        {
            Request = new ClientConnectionRequest ();
        }

        public static implicit operator ClientRequestingConnectionEventArgs (ClientRequestingConnectionEventArgs<TIn,TOut> args)
        {
            return new ClientRequestingConnectionEventArgs (args.Client, args.Request);
        }
    }
}
