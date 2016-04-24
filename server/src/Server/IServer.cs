using System;
using System.Collections.Generic;

namespace KRPC.Server
{
    /// <summary>
    /// A non-generic server.
    /// </summary>
    interface IServer
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
        /// <value>The address.</value>
        string Address { get; }

        /// <summary>
        /// Returns true if the server is running and accepting client connections.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Clients that are connected to the server.
        /// </summary>
        IEnumerable<IClient> Clients { get; }

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
        event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;
        event EventHandler<ClientConnectedArgs> OnClientConnected;
        event EventHandler<ClientActivityArgs> OnClientActivity;
        event EventHandler<ClientDisconnectedArgs> OnClientDisconnected;
    }

    /// <summary>
    /// A generic server, that receives values of type TIn from clients and
    /// sends values of type TOut to them.
    /// </summary>
    interface IServer<TIn,TOut>
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
        /// <value>The address.</value>
        string Address { get; }

        /// <summary>
        /// Returns true if the server is running and accepting client connections.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Clients that are connected to the server.
        /// </summary>
        IEnumerable<IClient<TIn,TOut>> Clients { get; }

        /// <summary>
        /// Gets the total number of bytes read from client streams.
        /// </summary>
        ulong BytesRead { get; }

        /// <summary>
        /// Gets the total number of bytes written to client streams.
        /// </summary>
        ulong BytesWritten { get; }

        /// <summary>
        /// Clear the bytes read and bytes written counts.
        /// </summary>
        void ClearStats ();

        event EventHandler OnStarted;
        event EventHandler OnStopped;
        event EventHandler<ClientRequestingConnectionArgs<TIn,TOut>> OnClientRequestingConnection;
        event EventHandler<ClientConnectedArgs<TIn,TOut>> OnClientConnected;
        event EventHandler<ClientActivityArgs<TIn,TOut>> OnClientActivity;
        event EventHandler<ClientDisconnectedArgs<TIn,TOut>> OnClientDisconnected;
    }
}
