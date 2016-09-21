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
        internal ServerEventArgs (Server server)
        {
            Server = server;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public Server Server { get; }
    }
}
