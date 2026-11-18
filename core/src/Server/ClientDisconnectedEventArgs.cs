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
    public sealed class ClientDisconnectedEventArgs<TStream> : ClientEventArgs<TStream>
        where TStream : IStream
    {
        internal ClientDisconnectedEventArgs (IClient<TStream> client) : base (client)
        {
        }
    }
}
