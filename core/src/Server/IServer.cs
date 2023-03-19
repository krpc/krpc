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

        event EventHandler OnStarted;
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

        event EventHandler<ClientRequestingConnectionEventArgs> OnClientRequestingConnection;
        event EventHandler<ClientConnectedEventArgs> OnClientConnected;
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

        event EventHandler<ClientRequestingConnectionEventArgs<TIn,TOut>> OnClientRequestingConnection;
        event EventHandler<ClientConnectedEventArgs<TIn,TOut>> OnClientConnected;
        event EventHandler<ClientDisconnectedEventArgs<TIn,TOut>> OnClientDisconnected;
    }
}
