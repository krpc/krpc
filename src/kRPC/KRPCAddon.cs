using System;
using System.Diagnostics;
using System.Reflection;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;
using KSP.IO;
using KRPC.Server;
using KRPC.Server.Net;
using KRPC.Server.RPC;
using KRPC.Service;
using KRPC.Schema.RPC;
using KRPC.Utils;
using KRPC.UI;

namespace KRPC
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    sealed public class KRPCAddon : MonoBehaviour
    {
        private static RPCServer server = null;
        private static TCPServer tcpServer = null;
        private MainWindow mainWindow;
        private KRPCConfiguration config;
        private IScheduler<IClient<Request,Response>> requestScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();

        public void Awake ()
        {
            //TODO: fails due to native code not being available
//            Logger.WriteLine ("Local addresses of available network adapters:");
//            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
//                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
//                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
//                        Logger.WriteLine ("   " + unicastIPAddressInformation.Address.ToString());
//                    }
//                }
//            }

            config = new KRPCConfiguration ();
            tcpServer = new TCPServer (config.Address, config.Port);
            server = new RPCServer (tcpServer);
            server.OnClientConnected += (sender, e) => requestScheduler.Add(e.Client);
            server.OnClientDisconnected += (sender, e) => requestScheduler.Remove(e.Client);
            mainWindow = gameObject.AddComponent<MainWindow>();
            mainWindow.OnStartServerPressed += StartServer;
            mainWindow.OnStopServerPressed += StopServer;
            mainWindow.Init(server);
        }

        public void OnDestroy ()
        {
            if (server.Running)
                server.Stop ();
        }

        private void StartServer (object sender, EventArgs args)
        {
            tcpServer.Port = config.Port;
            tcpServer.Address = config.Address;
            server.Start ();
        }

        private void StopServer (object sender, EventArgs args)
        {
            server.Stop ();
        }

        public void Update ()
        {
            if (server.Running) {
                // TODO: is there a better way to limit the number of requests handled per update?
                int threshold = 20; // milliseconds
                server.Update ();

                if (server.Clients.Count () > 0) {
                    Stopwatch timer = Stopwatch.StartNew ();
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
                                    response = Services.HandleRequest (Assembly.GetExecutingAssembly (), "KRPC.Service", request);
                                } catch (Exception e) {
                                    response = Response.CreateBuilder ();
                                    response.Error = true;
                                    response.ErrorMessage = e.ToString ();
                                    Logger.WriteLine (e.ToString ());
                                }

                                // Send response
                                response.SetTime (Planetarium.GetUniversalTime ());
                                var builtResponse = response.Build ();
                                //TODO: handle partial response exception
                                client.Stream.Write (builtResponse);
                                if (response.Error)
                                    Logger.WriteLine ("Sent error response to client " + client.Address + " (" + response.ErrorMessage + ")");
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
}
