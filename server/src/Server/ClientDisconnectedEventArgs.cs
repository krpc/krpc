namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    sealed class ClientDisconnectedEventArgs : ClientEventArgs
    {
        public ClientDisconnectedEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    sealed class ClientDisconnectedEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        public ClientDisconnectedEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
