using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    /// <summary>
    /// A client's session on a connection. A reconnect on the same connection is a new client.
    /// </summary>
    sealed class Client : IClient<Request,Response>
    {
        readonly Connection connection;
        readonly Stream stream;

        public Client (string name, uint protocolVersion, Connection innerConnection)
        {
            Name = name;
            ProtocolVersion = protocolVersion;
            Guid = Guid.NewGuid ();
            connection = innerConnection;
            stream = new Stream (this, connection);
        }

        public string Name { get; private set; }

        /// <summary>
        /// The protocol version sent by the client.
        /// </summary>
        public uint ProtocolVersion { get; private set; }

        public Guid Guid { get; private set; }

        public string Address {
            get { return connection.Client.Address; }
        }

        public IStream<Request,Response> Stream {
            get { return stream; }
        }

        /// <summary>
        /// Whether the client is connected, with its session pending approval or started.
        /// </summary>
        public bool Connected {
            get { return connection.IsCurrent (this) && connection.Client.Connected; }
        }

        /// <summary>
        /// End the session, as when the player removes the client from the server window.
        /// </summary>
        public void Close ()
        {
            connection.Disconnect (this, DisconnectReason.Removed, "The client was removed from the server");
        }

        public override bool Equals (object obj)
        {
            return ReferenceEquals (this, obj);
        }

        public bool Equals (IClient<Request,Response> other)
        {
            return ReferenceEquals (this, other);
        }

        public override int GetHashCode ()
        {
            return Guid.GetHashCode ();
        }
    }
}
