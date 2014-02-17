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
using KRPC.Schema.KRPC;
using KRPC.Utils;
using KRPC.UI;

namespace KRPC
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    sealed public class KRPCAddon : MonoBehaviour
    {
        private static RPCServer server = null;
        private static TCPServer tcpServer = null;
        private IButton toolbarButton;
        private MainWindow mainWindow;
        private ClientConnectingDialog clientConnectingDialog;
        private KRPCConfiguration config;
        private IScheduler<IClient<Request,Response>> requestScheduler;

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

            config = new KRPCConfiguration ("settings.cfg");
            config.Load ();
            tcpServer = new TCPServer (config.Address, config.Port);
            server = new RPCServer (tcpServer);
            requestScheduler = new RoundRobinScheduler<IClient<Request,Response>> ();
            server.OnClientConnected += (sender, e) => requestScheduler.Add(e.Client);
            server.OnClientDisconnected += (sender, e) => requestScheduler.Remove(e.Client);

            // Create main window
            mainWindow = gameObject.AddComponent<MainWindow>();
            mainWindow.Server = server;
            mainWindow.Visible = config.MainWindowVisible;
            mainWindow.Position = config.MainWindowPosition;

            // Create new connection dialog
            clientConnectingDialog = gameObject.AddComponent<ClientConnectingDialog>();

            // Main window events
            mainWindow.OnStartServerPressed += (s, e) => {
                tcpServer.Port = config.Port;
                tcpServer.Address = config.Address;
                server.Start ();
            };
            mainWindow.OnStopServerPressed += (s, e) => {
                server.Stop ();
                clientConnectingDialog.Close();
            };
            mainWindow.OnHide += (s, e) => {
                config.MainWindowVisible = false;
                config.Save ();
            };
            mainWindow.OnShow += (s, e) => {
                config.MainWindowVisible = true;
                config.Save ();
            };
            mainWindow.OnMoved += (s, e) => {
                var window = s as MainWindow;
                config.MainWindowPosition = window.Position;
                config.Save ();
            };

            // Server events
            server.OnClientRequestingConnection += clientConnectingDialog.OnClientRequestingConnection;

            // Toolbar API
            if (ToolbarManager.ToolbarAvailable) {
                toolbarButton = ToolbarManager.Instance.add ("kRPC", "ToggleMainWindow");
                toolbarButton.TexturePath = "kRPC/icon-offline";
                toolbarButton.ToolTip = "kRPC Server";
                toolbarButton.Visibility = new GameScenesVisibility (GameScenes.FLIGHT);
                toolbarButton.OnClick += (e) => {
                    mainWindow.Visible = !mainWindow.Visible;
                };
            } else {
                // If there is no toolbar button a hidden window can't be shown, so force it to be displayed
                mainWindow.Visible = true;
            }
        }

        public void OnDestroy ()
        {
            if (server.Running)
                server.Stop ();
            toolbarButton.Destroy ();
            UnityEngine.Object.Destroy (mainWindow);
            UnityEngine.Object.Destroy (clientConnectingDialog);
        }

        public void Update ()
        {
            // TODO: add server start/stop events to IServer and attach these updates to the handlers
            if (toolbarButton != null) {
                if (server.Running)
                    toolbarButton.TexturePath = "kRPC/icon-online";
                else
                    toolbarButton.TexturePath = "kRPC/icon-offline";
            }

            if (server.Running) {
                // TODO: is there a better way to limit the number of requests handled per update?
                int threshold = 20; // milliseconds
                server.Update ();

                if (server.Clients.Count () > 0 && !requestScheduler.Empty) {
                    Stopwatch timer = Stopwatch.StartNew ();
                    try {
                        do {
                            // Get request
                            IClient<Request,Response> client = requestScheduler.Next ();
                            if (client.Stream.DataAvailable) {
                                Request request = client.Stream.Read ();
                                mainWindow.SawClientActivity (client);
                                Logger.WriteLine ("Received request from client " + client.Address + " (" + request.Service + "." + request.Method + ")");

                                // Handle the request
                                Response.Builder response;
                                try {
                                    response = Service.Services.HandleRequest (request);
                                } catch (Exception e) {
                                    response = Response.CreateBuilder ();
                                    response.Error = e.ToString ();
                                    Logger.WriteLine (e.ToString ());
                                }

                                // Send response
                                response.SetTime (Planetarium.GetUniversalTime ());
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
}
