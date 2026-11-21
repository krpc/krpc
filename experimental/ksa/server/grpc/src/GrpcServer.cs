using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Grpc.AspNetCore.Server.Model;
using KRPC.Server;
using KRPC.Service.Messages;
using KRPC.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace KRPC.KSA.Grpc
{
    /// <summary>
    /// A server for gRPC clients, with one kRPC client per HTTP/2 connection. Kestrel runs the
    /// calls on its own threads. They reach the server core through the queues read in
    /// <see cref="Update"/>, on the game thread.
    /// </summary>
    public sealed class GrpcServer : IMessageServer
    {
        /// <summary>
        /// Create a server core server for gRPC clients.
        /// </summary>
        public static KRPC.Server.Server Create (Guid id, Protocol protocol, string name, IPAddress address, ushort port)
        {
            return new KRPC.Server.Server (id, protocol, name, new GrpcServer (address, port));
        }

        /// <summary>
        /// Event triggered when the server starts.
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event triggered when the server stops.
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event triggered when a client requests a connection.
        /// </summary>
        public event EventHandler<ClientRequestingConnectionEventArgs<IMessageStream>> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a client has connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<IMessageStream>> OnClientConnected;

        /// <summary>
        /// Event triggered when a client has disconnected.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<IMessageStream>> OnClientDisconnected;

        readonly IPAddress address;
        readonly ushort port;
        WebApplication app;

        // Written by Kestrel threads
        readonly ConcurrentDictionary<string, GrpcClient> connections = new ConcurrentDictionary<string, GrpcClient> ();
        readonly ConcurrentQueue<GrpcClient> connecting = new ConcurrentQueue<GrpcClient> ();
        readonly ConcurrentQueue<GrpcClient> disconnecting = new ConcurrentQueue<GrpcClient> ();

        // Game thread only
        readonly List<GrpcClient> pending = new List<GrpcClient> ();
        readonly Dictionary<Guid, GrpcClient> clients = new Dictionary<Guid, GrpcClient> ();
        ulong closedBytesRead;
        ulong closedBytesWritten;

        GrpcServer (IPAddress address, ushort port)
        {
            this.address = address;
            this.port = port;
        }

        /// <summary>
        /// Start listening for connections.
        /// </summary>
        public void Start ()
        {
            if (app != null)
                return;
            var methods = new ProcedureMethods (this);
            var builder = WebApplication.CreateSlimBuilder ();
            builder.Logging.ClearProviders ();
            builder.WebHost.ConfigureKestrel (options =>
                options.Listen (address, port, listen => {
                    // h2c, as gRPC clients connect without TLS to a plaintext address
                    listen.Protocols = HttpProtocols.Http2;
                    listen.Use (next => async connection => {
                        try {
                            await next (connection);
                        } finally {
                            ConnectionClosed (connection.ConnectionId);
                        }
                    });
                }));
            builder.Services.AddGrpc ();
            builder.Services.TryAddEnumerable (
                ServiceDescriptor.Singleton<IServiceMethodProvider<ProcedureMethods.Service>> (methods));
            app = builder.Build ();
            app.MapGrpcService<ProcedureMethods.Service> ();
            try {
                app.StartAsync ().GetAwaiter ().GetResult ();
            } catch (System.Exception e) {
                app = null;
                throw new ServerException (e.Message);
            }
            Logger.WriteLine ("gRPC server listening on " + Address + ", " + methods.Count + " methods");
            EventHandlerExtensions.Invoke (OnStarted, this);
        }

        /// <summary>
        /// Stop the server, ending every connection.
        /// </summary>
        public void Stop ()
        {
            if (app == null)
                return;
            app.StopAsync ().GetAwaiter ().GetResult ();
            app = null;
            Update ();
            foreach (var client in pending)
                client.Deny ();
            pending.Clear ();
            foreach (var client in new List<GrpcClient> (clients.Values))
                Disconnected (client);
            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        /// <summary>
        /// Raise the connection and disconnection events queued since the last update.
        /// </summary>
        public void Update ()
        {
            while (connecting.TryDequeue (out var client))
                pending.Add (client);
            for (int i = 0; i < pending.Count;) {
                var client = pending [i];
                if (RequestConnection (client))
                    pending.RemoveAt (i);
                else
                    i++;
            }
            while (disconnecting.TryDequeue (out var client)) {
                pending.Remove (client);
                if (clients.ContainsKey (client.Guid))
                    Disconnected (client);
            }
        }

        /// <summary>
        /// Ask the handlers whether to allow a client. Returns false while the request is
        /// still pending.
        /// </summary>
        bool RequestConnection (GrpcClient client)
        {
            var args = new ClientRequestingConnectionEventArgs<IMessageStream> (client);
            var handler = OnClientRequestingConnection;
            if (handler == null)
                args.Request.Allow ();
            else
                handler (this, args);
            if (args.Request.ShouldAllow) {
                clients [client.Guid] = client;
                client.Allow ();
                EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<IMessageStream> (client));
                return true;
            }
            if (args.Request.ShouldDeny) {
                client.Deny ();
                return true;
            }
            return false;
        }

        void Disconnected (GrpcClient client)
        {
            client.End ();
            clients.Remove (client.Guid);
            closedBytesRead += client.GrpcStream.BytesRead;
            closedBytesWritten += client.GrpcStream.BytesWritten;
            EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<IMessageStream> (client));
        }

        /// <summary>
        /// The client for a connection, created by the first call made over it. Called from a
        /// Kestrel thread.
        /// </summary>
        internal GrpcClient GetClient (string connectionId, string name, string peer)
        {
            return connections.GetOrAdd (connectionId, _ => {
                var client = new GrpcClient (name, peer, this);
                connecting.Enqueue (client);
                return client;
            });
        }

        void ConnectionClosed (string connectionId)
        {
            if (connections.TryRemove (connectionId, out var client)) {
                client.Disconnect ();
                disconnecting.Enqueue (client);
                Arrivals.Set ();
            }
        }

        /// <summary>
        /// End a client's session on the server's next update.
        /// </summary>
        internal void Remove (GrpcClient client)
        {
            client.End ();
            disconnecting.Enqueue (client);
            Arrivals.Set ();
        }

        /// <summary>
        /// Send the held back responses. A response completes its call as it is written, so
        /// none are held back.
        /// </summary>
        public void Flush ()
        {
        }

        /// <summary>
        /// The address the server listens on.
        /// </summary>
        public string Address {
            get { return address + ":" + port; }
        }

        /// <summary>
        /// Information about the server.
        /// </summary>
        public string Info {
            get { return "gRPC on " + Address; }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return app != null; }
        }

        /// <summary>
        /// The connected clients.
        /// </summary>
        public IEnumerable<IClient<IMessageStream>> Clients {
            get { return clients.Values; }
        }

        /// <summary>
        /// The number of request bytes received.
        /// </summary>
        public ulong BytesRead {
            get {
                var read = closedBytesRead;
                foreach (var client in clients.Values)
                    read += client.GrpcStream.BytesRead;
                return read;
            }
        }

        /// <summary>
        /// The number of response bytes sent.
        /// </summary>
        public ulong BytesWritten {
            get {
                var written = closedBytesWritten;
                foreach (var client in clients.Values)
                    written += client.GrpcStream.BytesWritten;
                return written;
            }
        }

        /// <summary>
        /// Clear the byte counts.
        /// </summary>
        public void ClearStats ()
        {
            closedBytesRead = 0;
            closedBytesWritten = 0;
            foreach (var client in clients.Values)
                client.GrpcStream.ClearStats ();
        }
    }
}
