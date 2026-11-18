using System;
using System.Collections.Generic;

namespace KRPC.Server
{
    /// <summary>
    /// A server whose clients communicate over streams of the given type: of bytes, or of
    /// messages.
    /// </summary>
    public interface IServer<TStream>
        where TStream : IStream
    {
        /// <summary>
        /// Start the server.
        /// </summary>
        void Start ();

        /// <summary>
        /// Stop the server.
        /// </summary>
        void Stop ();

        /// <summary>
        /// Update the server. Call this regularly to ensure timely handling
        /// of new client connections and other functionality.
        /// </summary>
        void Update ();

        /// <summary>
        /// The servers address.
        /// </summary>
        string Address { get; }

        /// <summary>
        /// Information about the server, displayed in the UI.
        /// </summary>
        string Info { get; }

        /// <summary>
        /// Returns true if the server is running and accepting client connections.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// The total number of bytes read by the server.
        /// </summary>
        ulong BytesRead { get; }

        /// <summary>
        /// The total number of bytes written by the server.
        /// </summary>
        ulong BytesWritten { get; }

        /// <summary>
        /// Clear the bytes read and bytes written counts.
        /// </summary>
        void ClearStats ();

        /// <summary>
        /// Handler to trigger when the server starts.
        /// </summary>
        event EventHandler OnStarted;

        /// <summary>
        /// Handler to trigger when the server stops.
        /// </summary>
        event EventHandler OnStopped;

        /// <summary>
        /// Clients that are connected to the server.
        /// </summary>
        IEnumerable<IClient<TStream>> Clients { get; }

        /// <summary>
        /// Handler to trigger when a client has connected.
        /// </summary>
        event EventHandler<ClientConnectedEventArgs<TStream>> OnClientConnected;

        /// <summary>
        /// Handler to trigger when a client has disconnected.
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs<TStream>> OnClientDisconnected;
    }

    /// <summary>
    /// A server whose clients communicate in messages, and ask for their connection to be
    /// approved.
    /// </summary>
    public interface IMessageServer : IServer<IMessageStream>
    {
        /// <summary>
        /// Handler to trigger when a new client requests a connection.
        /// </summary>
        event EventHandler<ClientRequestingConnectionEventArgs<IMessageStream>> OnClientRequestingConnection;

        /// <summary>
        /// Send the responses held back while their clients had more requests queued.
        /// </summary>
        void Flush ();
    }
}
