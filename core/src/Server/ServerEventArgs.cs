using System;

namespace KRPC.Server
{
    /// <summary>
    /// Abstract base class for a server event.
    /// </summary>
    public class ServerEventArgs : EventArgs
    {
        /// <summary>
        /// A client event for the given client.
        /// </summary>
        public ServerEventArgs (Server server)
        {
            Server = server;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public Server Server { get; }
    }
}
