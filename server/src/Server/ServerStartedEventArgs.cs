namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a server started event
    /// </summary>
    public sealed class ServerStartedEventArgs : ServerEventArgs
    {
        internal ServerStartedEventArgs (Server server) : base (server)
        {
        }
    }
}
