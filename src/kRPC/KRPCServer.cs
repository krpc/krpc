using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Server.RPC;
using KRPC.Server.Stream;
using KRPC.Schema.KRPC;
using KRPC.Service;
using KRPC.Continuations;
using KRPC.Utils;

namespace KRPC
{
    /// <summary>
    /// The kRPC server
    /// </summary>
    public class KRPCServer : IServer
    {
        readonly TCPServer rpcTcpServer;
        readonly TCPServer streamTcpServer;
        readonly RPCServer rpcServer;
        readonly StreamServer streamServer;
        IScheduler<IClient<Request,Response>> clientScheduler;
        IList<RequestContinuation> continuations;
        IDictionary<IClient<byte,StreamMessage>, IList<StreamRequest>> streamRequests;

        internal delegate double UniversalTimeFunction ();

        internal UniversalTimeFunction GetUniversalTime;

        /// <summary>
        /// Event triggered when the server starts
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event triggered when the server stops
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event triggered when a client is requesting a connection
        /// </summary>
        public event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;

        /// <summary>
        /// Event triggered when a client has connected
        /// </summary>
        public event EventHandler<ClientConnectedArgs> OnClientConnected;

        /// <summary>
        /// Event triggered when a client performs some activity
        /// </summary>
        public event EventHandler<ClientActivityArgs> OnClientActivity;

        /// <summary>
        /// Event triggered when a client has disconnected
        /// </summary>
        public event EventHandler<ClientDisconnectedArgs> OnClientDisconnected;

        /// <summary>
        /// Stores the context in which a continuation is executed.
        /// For example, used by a continuation to find out which client made the request.
        /// </summary>
        public static class Context
        {
            /// <summary>
            /// The server instance
            /// </summary>
            public static KRPCServer Server { get; private set; }

            /// <summary>
            /// The current client
            /// </summary>
            public static IClient RPCClient { get; private set; }

            /// <summary>
            /// The current game scene
            /// </summary>
            public static GameScene GameScene { get; private set; }

            internal static void Set (KRPCServer server, IClient rpcClient)
            {
                Server = server;
                RPCClient = rpcClient;
            }

            internal static void Clear ()
            {
                Server = null;
                RPCClient = null;
            }

            internal static void SetGameScene (GameScene gameScene)
            {
                GameScene = gameScene;
            }
        }

        internal KRPCServer (IPAddress address, ushort rpcPort, ushort streamPort)
        {
            rpcTcpServer = new TCPServer ("RPCServer", address, rpcPort);
            streamTcpServer = new TCPServer ("StreamServer", address, streamPort);
            rpcServer = new RPCServer (rpcTcpServer);
            streamServer = new StreamServer (streamTcpServer);
            clientScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();
            continuations = new List<RequestContinuation> ();
            streamRequests = new Dictionary<IClient<byte,StreamMessage>,IList<StreamRequest>> ();

            // Tie events to underlying server
            rpcServer.OnStarted += (s, e) => {
                if (OnStarted != null)
                    OnStarted (this, EventArgs.Empty);
            };
            rpcServer.OnStopped += (s, e) => {
                if (OnStopped != null)
                    OnStopped (this, EventArgs.Empty);
            };
            rpcServer.OnClientRequestingConnection += (s, e) => {
                if (OnClientRequestingConnection != null)
                    OnClientRequestingConnection (s, e);
            };
            rpcServer.OnClientConnected += (s, e) => {
                if (OnClientConnected != null)
                    OnClientConnected (s, e);
            };
            rpcServer.OnClientDisconnected += (s, e) => {
                if (OnClientDisconnected != null)
                    OnClientDisconnected (s, e);
            };

            // Add/remove clients from the scheduler
            rpcServer.OnClientConnected += (s, e) => clientScheduler.Add (e.Client);
            rpcServer.OnClientDisconnected += (s, e) => clientScheduler.Remove (e.Client);

            // Add/remove clients from the list of stream requests
            streamServer.OnClientConnected += (s, e) => streamRequests [e.Client] = new List<StreamRequest> ();
            streamServer.OnClientDisconnected += (s, e) => streamRequests.Remove (e.Client);

            // Validate stream client identifiers
            streamServer.OnClientRequestingConnection += (s, e) => {
                if (rpcServer.Clients.Where (c => c.Guid == e.Client.Guid).Any ())
                    e.Request.Allow ();
                else
                    e.Request.Deny ();
            };
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start ()
        {
            rpcServer.Start ();
            streamServer.Start ();
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop ()
        {
            rpcServer.Stop ();
            streamServer.Stop ();
            ObjectStore.Clear ();
        }

        /// <summary>
        /// Get/set the servers listen address
        /// </summary>
        public IPAddress Address {
            get { return rpcTcpServer.Address; }
            set {
                rpcTcpServer.Address = value;
                streamTcpServer.Address = value;
            }
        }

        /// <summary>
        /// Get/set the RPC port
        /// </summary>
        public ushort RPCPort {
            get { return rpcTcpServer.Port; }
            set { rpcTcpServer.Port = value; }
        }

        /// <summary>
        /// Get/set the Stream port
        /// </summary>
        public ushort StreamPort {
            get { return streamTcpServer.Port; }
            set { streamTcpServer.Port = value; }
        }

        /// <summary>
        /// Returns true if the server is running
        /// </summary>
        public bool Running {
            get { return rpcServer.Running && streamServer.Running; }
        }

        /// <summary>
        /// Returns a list of clients the server knows about. Note that they might
        /// not be connected to the server.
        /// </summary>
        public IEnumerable<IClient> Clients {
            get { return rpcServer.Clients.Select (x => (IClient)x); }
        }

        /// <summary>
        /// Update the server
        /// </summary>
        public void Update ()
        {
            RPCServerUpdate ();
            StreamServerUpdate ();
        }

        /// <summary>
        /// Update the RPC server
        /// </summary>
        void RPCServerUpdate ()
        {
            // The maximum amount of time to spend executing continuations
            const int maxTime = 10; // milliseconds
            // The maximum amount of time to wait after executing continuations to check for new requests
            const int timeout = 1; // milliseconds
            var done = false;
            var waited = false;
            var yieldedContinuations = new List<RequestContinuation> ();

            Stopwatch timer = Stopwatch.StartNew ();
            do {
                // Check for new requests from clients
                rpcServer.Update ();
                streamServer.Update ();
                PollRequests (yieldedContinuations);

                // Process pending continuations (client requests)
                if (continuations.Count > 0) {
                    foreach (var continuation in continuations) {
                        // Ignore the continuation if the client has disconnected
                        if (!continuation.Client.Connected)
                            continue;
                        // Execute the continuation
                        try {
                            ExecuteContinuation (continuation);
                        } catch (YieldException e) {
                            yieldedContinuations.Add ((RequestContinuation)e.Continuation);
                        }
                    }
                    continuations.Clear ();
                }

                if (timer.ElapsedMilliseconds > maxTime) {
                    done = true;
                } else if (!waited && continuations.Count == 0) {
                    Thread.Sleep (timeout);
                    waited = true;
                }
            } while (!done);

            // Run yielded continuations on the next update
            continuations = yieldedContinuations;
        }

        /// <summary>
        /// Update the Stream server
        /// </summary>
        void StreamServerUpdate ()
        {
            streamServer.Update ();

            // Run streaming requests
            foreach (var entry in streamRequests) {
                var streamClient = entry.Key;
                var requests = entry.Value;
                if (!requests.Any ())
                    continue;
                var streamMessage = StreamMessage.CreateBuilder ();
                foreach (var request in requests) {
                    Response.Builder response;
                    try {
                        response = KRPC.Service.Services.Instance.HandleRequest (request.Procedure, request.Arguments);
                    } catch (Exception e) {
                        response = Response.CreateBuilder ();
                        response.SetError (e.ToString ());
                    }
                    response.SetTime (GetUniversalTime ());
                    var builtResponse = response.Build ();
                    var streamResponse = request.ResponseBuilder;
                    streamResponse.SetResponse (builtResponse);
                    streamMessage.AddResponses (streamResponse);
                }
                streamClient.Stream.Write (streamMessage.Build ());
            }
        }

        internal uint AddStream (IClient client, Request request)
        {
            var streamClient = streamServer.Clients.Single (c => c.Guid == client.Guid);

            // Check for an existing stream for the request
            var procedure = KRPC.Service.Services.Instance.GetProcedureSignature (request);
            var arguments = KRPC.Service.Services.Instance.DecodeArguments (procedure, request);
            foreach (var streamRequest in streamRequests[streamClient]) {
                if (streamRequest.Procedure == procedure && streamRequest.Arguments.SequenceEqual (arguments))
                    return streamRequest.Identifier;
            }

            // Create a new stream
            {
                var streamRequest = new StreamRequest (request);
                streamRequests [streamClient].Add (streamRequest);
                return streamRequest.Identifier;
            }
        }

        internal void RemoveStream (IClient client, uint identifier)
        {
            var streamClient = streamServer.Clients.Single (c => c.Guid == client.Guid);
            var requests = streamRequests [streamClient].Where (x => x.Identifier == identifier).ToList ();
            if (!requests.Any ())
                return;
            streamRequests [streamClient].Remove (requests.Single ());
        }

        /// <summary>
        /// Poll connected clients for new requests.
        /// Adds a continuation to the queue for any client with a new request,
        /// if a continuation is not already being processed for the client.
        /// </summary>
        void PollRequests (IEnumerable<RequestContinuation> yieldedContinuations)
        {
            var currentClients = continuations.Select (((c) => c.Client)).ToList ();
            currentClients.AddRange (yieldedContinuations.Select (((c) => c.Client)));
            foreach (var client in clientScheduler) {
                if (!currentClients.Contains (client) && client.Stream.DataAvailable) {
                    Request request = client.Stream.Read ();
                    if (OnClientActivity != null)
                        OnClientActivity (this, new ClientActivityArgs (client));
                    if (Logger.ShouldLog (Logger.Severity.Debug))
                        Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Procedure + ")", Logger.Severity.Debug);
                    continuations.Add (new RequestContinuation (client, request));
                }
            }
        }

        /// <summary>
        /// Execute the continuation and send a response to the client,
        /// or throw a YieldException if the continuation is not complete.
        /// </summary>
        void ExecuteContinuation (RequestContinuation continuation)
        {
            var client = continuation.Client;

            // Run the continuation, and either return a result, an error,
            // or throw a YieldException if the continuation has not completed
            Response.Builder response;
            try {
                Context.Set (this, client);
                response = continuation.Run ();
            } catch (YieldException) {
                throw;
            } catch (Exception e) {
                response = Response.CreateBuilder ();
                response.Error = e.Message;
                if (Logger.ShouldLog (Logger.Severity.Debug))
                    Logger.WriteLine (e.Message, Logger.Severity.Debug);
            } finally {
                Context.Clear ();
            }

            // Send response to the client
            response.SetTime (GetUniversalTime ());
            var builtResponse = response.Build ();
            ((RPCClient)client).Stream.Write (builtResponse);
            if (Logger.ShouldLog (Logger.Severity.Debug)) {
                if (response.HasError)
                    Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")", Logger.Severity.Debug);
                else
                    Logger.WriteLine ("Sent response to client " + client.Address, Logger.Severity.Debug);
            }
        }
    }
}
