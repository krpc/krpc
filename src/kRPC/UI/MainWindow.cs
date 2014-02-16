using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;
using KSP;
using KRPC.Server.RPC;
using KRPC.Server.Net;

namespace KRPC.UI
{
    sealed class MainWindow : MonoBehaviour
    {
        private int windowId = UnityEngine.Random.Range(1000, 2000000);
        private Rect windowPosition = new Rect();
        private bool hasInitStyles = false;
        private GUIStyle windowStyle, labelStyle;
        private RPCServer server;

        public event EventHandler OnStartServerPressed;
        public event EventHandler OnStopServerPressed;

        public void Init(RPCServer server) {
            this.server = server;
        }

        public void Awake() {
            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        private void InitStyles() {
            if (!hasInitStyles) {
                windowStyle = new GUIStyle(UnityEngine.GUI.skin.window);
                windowStyle.fixedWidth = 250f;
                labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                labelStyle.stretchWidth = true;
                hasInitStyles = true;
            }
        }

        private void DrawGUI() {
            InitStyles ();
            windowPosition = GUILayout.Window (windowId, windowPosition, DrawWindow, "kRPC Server", windowStyle);
        }

        private void DrawWindow(int windowID) {
            GUILayout.BeginVertical();
            GUILayout.Label ("Server status: " + (server.Running ? "Online" : "Offline"), labelStyle);
            if (server.Running) {
                //FIXME: use rpcserver not tcpserver
                if (GUILayout.Button ("Stop server"))
                    OnStopServerPressed (this, EventArgs.Empty);
                TCPServer tcpServer = (TCPServer)server.Server;
                GUILayout.Label ("Server address: " + tcpServer.Address, labelStyle);
                GUILayout.Label ("Server port: " + tcpServer.Port, labelStyle);
                GUILayout.Label ("Allowed client(s): " + AllowedClientsString(tcpServer.Address), labelStyle);
                int numClients = tcpServer.Clients.Count ();
                GUILayout.Label (numClients + " client" + (numClients == 1 ? "" : "s") + " connected", labelStyle);
            } else {
                if (GUILayout.Button ("Start server"))
                    OnStartServerPressed (this, EventArgs.Empty);
            }
            GUILayout.EndVertical ();
            UnityEngine.GUI.DragWindow ();
        }

        private string AllowedClientsString(IPAddress localAddress) {
            if (localAddress.ToString() == "127.0.0.1")
                return "Local only";
            var subnet = GetSubnetMask (localAddress);
            if (subnet != null)
                return "Subnet mask " + subnet;
            return "?";
        }

        private static IPAddress GetSubnetMask(IPAddress address)
        {
            //TODO: fails due to native code not being available
//            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
//                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)    {
//                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
//                        if (address.Equals(unicastIPAddressInformation.Address)) {
//                            return unicastIPAddressInformation.IPv4Mask;
//                        }
//                    }
//                }
//            }
            return null;
        }
    }
}

