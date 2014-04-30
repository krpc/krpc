using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Server.RPC;
using KRPC.Schema.KRPC;
using KRPC.Service;
using KRPC.Continuations;
using KRPC.Utils;
using System.Diagnostics;

namespace KRPC
{
    class KRPCServer : IServer
    {
        readonly RPCServer rpcServer;
        readonly TCPServer tcpServer;
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

        public KRPCServer (IPAddress address, ushort port)
        {
            tcpServer = new TCPServer (address, port);
            rpcServer = new RPCServer (tcpServer);
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
        }

        public void Stop ()
        {
            rpcServer.Stop ();
        }

        public IPAddress Address {
            get { return tcpServer.Address; }
            set { tcpServer.Address = value; }
        }

        public ushort Port {
            get { return tcpServer.Port; }
            set { tcpServer.Port = value; }
        }

        public bool Running {
            get { return rpcServer.Running; }
        }

        public IEnumerable<IClient> Clients {
            get { return rpcServer.Clients.Select (x => (IClient)x); }
        }

        public void Update ()
        {
            // TODO: is there a better way to limit the number of requests handled per update?
            const int threshold = 20; // milliseconds

            Stopwatch timer = Stopwatch.StartNew ();
            do {
                rpcServer.Update ();

                // Check for new requests from clients
                PollRequests ();

                // Process pending continuations (client requests)
                if (continuations.Count > 0) {
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
            } while (timer.ElapsedMilliseconds < threshold && continuations.Count > 0);
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
            var request = continuation.Request;

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