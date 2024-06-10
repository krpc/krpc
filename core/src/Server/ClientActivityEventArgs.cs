namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    public sealed class ClientActivityEventArgs : ClientEventArgs
    {
        internal ClientActivityEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    public sealed class ClientActivityEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        internal ClientActivityEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
