using System;
using System.Collections.Generic;

namespace KRPC.Server
{
    /// <summary>
    /// A non-generic server.
    /// </summary>
    public interface IBaseServer
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
    }

    /// <summary>
    /// A non-generic server.
    /// </summary>
    public interface IServer : IBaseServer
    {
        /// <summary>
        /// Clients that are connected to the server.
        /// </summary>
        IEnumerable<IClient> Clients { get; }

        /// <summary>
        /// Handler to trigger when a new client requests a connection.
        /// </summary>
        event EventHandler<ClientRequestingConnectionEventArgs> OnClientRequestingConnection;

        /// <summary>
        /// Handler to trigger when a client has connected.
        /// </summary>
        event EventHandler<ClientConnectedEventArgs> OnClientConnected;

        /// <summary>
        /// Handler to trigger when a client has disconnected.
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;
    }

    /// <summary>
    /// A generic server, that receives values of type TIn from clients and
    /// sends values of type TOut to them.
    /// </summary>
    public interface IServer<TIn,TOut> : IBaseServer
    {
        /// <summary>
        /// Clients that are connected to the server.
        /// </summary>
        IEnumerable<IClient<TIn,TOut>> Clients { get; }

        /// <summary>
        /// Handler to trigger when a new client requests a connection.
        /// </summary>
        event EventHandler<ClientRequestingConnectionEventArgs<TIn,TOut>> OnClientRequestingConnection;

        /// <summary>
        /// Handler to trigger when a client has connected.
        /// </summary>
        event EventHandler<ClientConnectedEventArgs<TIn,TOut>> OnClientConnected;

        /// <summary>
        /// Handler to trigger when a client has disconnected.
        /// </summary>
        event EventHandler<ClientDisconnectedEventArgs<TIn,TOut>> OnClientDisconnected;
    }
}
