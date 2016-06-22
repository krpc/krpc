namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    sealed class ClientActivityEventArgs : ClientEventArgs
    {
        public ClientActivityEventArgs (IClient client) : base (client)
        {
        }
    }

    /// <summary>
    /// Arguments passed to a client activity event
    /// </summary>
    sealed class ClientActivityEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        public ClientActivityEventArgs (IClient<TIn,TOut> client) : base (client)
        {
        }
    }
}
