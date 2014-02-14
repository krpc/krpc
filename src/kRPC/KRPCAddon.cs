using System;
using System.Reflection;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;
using KSP.IO;
using KRPC.Server;
using KRPC.Service;
using KRPC.Schema.RPC;
using KRPC.Utils;
using KRPC.UI;

namespace KRPC
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KRPCAddon : MonoBehaviour
    {
        private static RPCServer server = null;
        private static TCPServer tcpServer = null;
        private MainWindow mainWindow;
        private KRPCConfiguration config;

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
            tcpServer = new TCPServer (config.LocalAddress, config.Port);
            server = new RPCServer (tcpServer);
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
            tcpServer.LocalAddress = config.LocalAddress;
            server.Start ();
        }

        private void StopServer (object sender, EventArgs args)
        {
            server.Stop ();
        }

        public void Update ()
        {
            if (server.Running) {
                try {
                    // Get request
                    Tuple<int,Request> request = server.GetRequest ();
                    Debug.Log("[kRPC] Received request from client " + request.Item1 + " (" + request.Item2.Service + "." + request.Item2.Method + ")");

                    // Handle the request
                    Response.Builder response;
                    try {
                        response = Services.HandleRequest (Assembly.GetExecutingAssembly (), "KRPC.Service", request.Item2);
                    } catch (Exception e) {
                        response = Response.CreateBuilder ()
                            .SetError (true)
                            .SetErrorMessage (e.ToString ());
                        Debug.Log (e.ToString ());
                    }
                    // Send response
                    response.SetTime (Planetarium.GetUniversalTime ());
                    var builtResponse = response.BuildPartial();
                    server.SendResponse (request.Item1, builtResponse);
                    if (response.Error)
                        Debug.Log("[kRPC] Sent error response to client " + request.Item1 + " (" + response.ErrorMessage + ")");
                    else
                        Debug.Log("[kRPC] Sent response to client " + request.Item1);
                } catch (NoRequestException) {
                }
            }
        }
    }
}
