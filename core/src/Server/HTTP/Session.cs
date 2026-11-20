using System;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// The one client of an HTTP server, which lasts while the server runs. Every request
    /// is a call on it.
    /// </summary>
    sealed class Session : IClient<IMessageStream>
    {
        readonly Server server;
        readonly SessionStream stream;

        public Session (Server httpServer)
        {
            server = httpServer;
            Guid = Guid.NewGuid ();
            stream = new SessionStream (httpServer);
        }

        public string Name {
            get { return "HTTP"; }
        }

        public Guid Guid { get; private set; }

        public string Address {
            get { return server.Address; }
        }

        public IMessageStream Stream {
            get { return stream; }
        }

        // A session from before a restart stays disconnected
        public bool Connected {
            get { return server.Running && server.IsSession (this); }
        }

        /// <summary>
        /// Does nothing. The session ends when the server stops.
        /// </summary>
        public void Close ()
        {
        }

        public override bool Equals (object obj)
        {
            return ReferenceEquals (this, obj);
        }

        public bool Equals (IClient<IMessageStream> other)
        {
            return ReferenceEquals (this, other);
        }

        public override int GetHashCode ()
        {
            return Guid.GetHashCode ();
        }
    }
}
