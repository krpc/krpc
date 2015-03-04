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
    class KRPCServer : IServer
    {
        readonly TCPServer rpcTcpServer;
        readonly TCPServer streamTcpServer;
        readonly RPCServer rpcServer;
        readonly StreamServer streamServer;
        IScheduler<IClient<Request,Response>> clientScheduler;
        //TODO: add maximum execution time for continuations to prevent livelock?
        IList<RequestContinuation> continuations;

        internal delegate double UniversalTimeFunction ();

        internal UniversalTimeFunction GetUniversalTime;

        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedArgs> OnClientConnected;
        public event EventHandler<ClientActivityArgs> OnClientActivity;
        public event EventHandler<ClientDisconnectedArgs> OnClientDisconnected;

        public KRPCServer (IPAddress address, ushort rpcPort, ushort streamPort)
        {
            rpcTcpServer = new TCPServer ("RPCServer", address, rpcPort);
            streamTcpServer = new TCPServer ("StreamServer", address, streamPort);
            rpcServer = new RPCServer (rpcTcpServer);
            streamServer = new StreamServer (streamTcpServer);
            clientScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();
            continuations = new List<RequestContinuation> ();

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
        }

        public void Start ()
        {
            rpcServer.Start ();
            streamServer.Start ();
        }

        public void Stop ()
        {
            rpcServer.Stop ();
            streamServer.Stop ();
        }

        public IPAddress Address {
            get { return rpcTcpServer.Address; }
            set {
                rpcTcpServer.Address = value;
                streamTcpServer.Address = value;
            }
        }

        public ushort RPCPort {
            get { return rpcTcpServer.Port; }
            set { rpcTcpServer.Port = value; }
        }

        public ushort StreamPort {
            get { return streamTcpServer.Port; }
            set { streamTcpServer.Port = value; }
        }

        public bool Running {
            get { return rpcServer.Running && streamServer.Running; }
        }

        public IEnumerable<IClient> Clients {
            get { return rpcServer.Clients.Select (x => (IClient)x); }
        }

        public void Update ()
        {
            // TODO: is there a better way to limit the number of requests handled per update?
            // The maximum amount of time to spend executing continuations
            const int maxTime = 10; // milliseconds
            // The maximum amount of time to wait after executing continuations to check for new requests
            const int timeout = 1; // milliseconds
            var done = false;
            var hadNewContinuations = false;

            Stopwatch timer = Stopwatch.StartNew ();
            do {
                rpcServer.Update ();
                streamServer.Update ();

                // Check for new requests from clients
                PollRequests ();

                // Process pending continuations (client requests)
                hadNewContinuations = false;
                if (continuations.Count > 0) {
                    hadNewContinuations = true;
                    var newContinuations = new List<RequestContinuation> ();
                    foreach (var continuation in continuations) {
                        try {
                            ExecuteContinuation (continuation);
                        } catch (YieldException e) {
                            // TODO: remove cast
                            newContinuations.Add ((RequestContinuation)e.Continuation);
                        }
                    }
                    continuations = newContinuations;
                }

                if (timer.ElapsedMilliseconds > maxTime) {
                    done = true;
                } else if (timeout > 0 && hadNewContinuations && continuations.Count == 0) {
                    Thread.Sleep (timeout);
                }
            } while (!done);
        }

        /// <summary>
        /// Poll connected clients for new requests.
        /// Adds a continuation to the queue for any client with a new request,
        /// if a continuation is not already being processed for the client.
        /// </summary>
        void PollRequests ()
        {
            var currentClients = continuations.Select (((c) => c.Client));
            foreach (var client in clientScheduler) {
                if (!currentClients.Contains (client) && client.Stream.DataAvailable) {
                    Request request = client.Stream.Read ();
                    if (OnClientActivity != null)
                        OnClientActivity (this, new ClientActivityArgs (client));
                    Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Procedure + ")");
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
                response = continuation.Run ();
            } catch (YieldException) {
                throw;
            } catch (Exception e) {
                response = Response.CreateBuilder ();
                response.Error = e.ToString ();
                Logger.WriteLine (e.ToString ());
            }

            // Send response to the client
            response.SetTime (GetUniversalTime ());
            var builtResponse = response.Build ();
            //TODO: handle partial response exception
            //TODO: remove cast
            ((RPCClient)client).Stream.Write (builtResponse);
            if (response.HasError)
                Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")");
            else
                Logger.WriteLine ("Sent response to client " + client.Address);
        }
    }
}