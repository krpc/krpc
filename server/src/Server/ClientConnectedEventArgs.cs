namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    sealed class ClientConnectedEventArgs : ClientEventArgs
    {
        public ClientConnectedEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    sealed class ClientConnectedEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        public ClientConnectedEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
