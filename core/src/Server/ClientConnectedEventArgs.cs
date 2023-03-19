namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    public sealed class ClientConnectedEventArgs : ClientEventArgs
    {
        internal ClientConnectedEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client connected event
    /// </summary>
    public sealed class ClientConnectedEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        internal ClientConnectedEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
