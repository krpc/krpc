namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a server started event
    /// </summary>
    public sealed class ServerStoppedEventArgs : ServerEventArgs
    {
        internal ServerStoppedEventArgs (Server server) : base (server)
        {
        }
    }
}
