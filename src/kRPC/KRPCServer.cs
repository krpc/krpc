using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Server.RPC;
using KRPC.Schema.KRPC;
using KRPC.Utils;
using System.Diagnostics;

namespace KRPC
{
    class KRPCServer : IServer
    {
        readonly RPCServer rpcServer;
        readonly TCPServer tcpServer;
        readonly IScheduler<IClient<Request,Response>> requestScheduler;

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
            requestScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();

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
            rpcServer.OnClientConnected += (s, e) => requestScheduler.Add (e.Client);
            rpcServer.OnClientDisconnected += (s, e) => requestScheduler.Remove (e.Client);
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
            rpcServer.Update ();

            if (rpcServer.Clients.Any () && !requestScheduler.Empty) {
                Stopwatch timer = Stopwatch.StartNew ();
                try {
                    do {
                        // Get request
                        IClient<Request,Response> client = requestScheduler.Next ();
                        if (client.Stream.DataAvailable) {
                            Request request = client.Stream.Read ();
                            if (OnClientActivity != null)
                                OnClientActivity (this, new ClientActivityArgs (client));
                            Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Procedure + ")");

                            // Handle the request
                            Response.Builder response;
                            try {
                                response = KRPC.Service.Services.Instance.HandleRequest (request);
                            } catch (Exception e) {
                                response = Response.CreateBuilder ();
                                response.Error = e.ToString ();
                                Logger.WriteLine (e.ToString ());
                            }

                            // Send response
                            response.SetTime (GetUniversalTime ());
                            var builtResponse = response.Build ();
                            //TODO: handle partial response exception
                            client.Stream.Write (builtResponse);
                            if (response.HasError)
                                Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")");
                            else
                                Logger.WriteLine ("Sent response to client " + client.Address);
                        }
                    } while (timer.ElapsedMilliseconds < threshold);
                } catch (NoRequestException) {
                }
            }
        }
    }
}

