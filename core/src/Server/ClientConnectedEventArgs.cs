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
    public sealed class ClientConnectedEventArgs<TStream> : ClientEventArgs<TStream>
        where TStream : IStream
    {
        internal ClientConnectedEventArgs (IClient<TStream> client) : base (client)
        {
        }
    }
}
