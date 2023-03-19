namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    public sealed class ClientDisconnectedEventArgs : ClientEventArgs
    {
        internal ClientDisconnectedEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client disconnected event
    /// </summary>
    sealed class ClientDisconnectedEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        internal ClientDisconnectedEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
