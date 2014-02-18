using System;
using System.Net;
using System.Linq;
using KRPC.Server.Net;
using KRPC.Server.RPC;
using KRPC.Utils;
using KRPC.Server;
using KRPC.Schema.KRPC;
using System.Diagnostics;

namespace TestServer
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            var tcpServer = new TCPServer (IPAddress.Loopback, 50000);
            var server = new RPCServer (tcpServer);
            var requestScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();
            server.OnClientConnected += (sender, e) => requestScheduler.Add(e.Client);
            server.OnClientDisconnected += (sender, e) => requestScheduler.Remove(e.Client);
            server.OnClientRequestingConnection += (s, e) => e.Allow ();
            server.Start ();
            Console.WriteLine ("Server started @ " + tcpServer.Address + ":" + tcpServer.Port);

            try {
                while (true) {
                    int interval = 100;
                    server.Update ();

                    Stopwatch timer = Stopwatch.StartNew ();
                    if (server.Clients.Count () > 0 && !requestScheduler.Empty) {
                        try {
                            do {
                                // Get request
                                IClient<Request,Response> client = requestScheduler.Next ();
                                if (client.Stream.DataAvailable) {
                                    Request request = client.Stream.Read ();
                                    Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Method + ")");

                                    // Handle the request
                                    Response.Builder response;
                                    try {
                                        response = KRPC.Service.Services.HandleRequest (request);
                                    } catch (Exception e) {
                                        response = Response.CreateBuilder ();
                                        response.Error = e.ToString ();
                                        Logger.WriteLine (e.ToString ());
                                    }

                                    // Send response
                                    // TODO: return the actual time?
                                    response.SetTime (42);
                                    var builtResponse = response.Build ();
                                    client.Stream.Write (builtResponse);
                                    if (response.HasError)
                                        Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.Error + ")");
                                    else
                                        Logger.WriteLine ("Sent response to client " + client.Address);
                                }
                            } while (timer.ElapsedMilliseconds < interval);
                        } catch (NoRequestException) {
                        }
                    }
                    int delay = interval - Math.Max((int)timer.ElapsedMilliseconds, interval);
                    if (delay > 0)
                        System.Threading.Thread.Sleep(delay);
                }
            } finally {
                server.Stop ();
            }
        }
    }
}
