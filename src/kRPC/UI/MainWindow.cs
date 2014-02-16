using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP;
using KRPC.Server;
using KRPC.Server.RPC;
using KRPC.Server.Net;

namespace KRPC.UI
{
    sealed class MainWindow : MonoBehaviour
    {
        private int windowId = UnityEngine.Random.Range(1000, 2000000);
        private Rect windowPosition = new Rect();
        private bool hasInitStyles = false;
        private GUIStyle windowStyle, labelStyle, activityStyle;
        private RPCServer server;
        private Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        public event EventHandler OnStartServerPressed;
        public event EventHandler OnStopServerPressed;

        public bool Visible { get; set; }

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
                activityStyle = new GUIStyle (HighLogic.Skin.toggle);
                activityStyle.active = HighLogic.Skin.toggle.normal;
                activityStyle.focused = HighLogic.Skin.toggle.normal;
                activityStyle.hover = HighLogic.Skin.toggle.normal;
                activityStyle.border = new RectOffset (0, 0, 0, 0);
                activityStyle.padding = new RectOffset (6, 0, 6, 0);
                activityStyle.overflow = new RectOffset (0, 0, 0, 0);
                activityStyle.imagePosition = ImagePosition.ImageOnly;
                hasInitStyles = true;
            }
        }

        private void DrawGUI() {
            InitStyles ();
            if (Visible) {
                windowPosition = GUILayout.Window (windowId, windowPosition, DrawWindow, "kRPC Server", windowStyle);
            }
        }

        private void DrawWindow(int windowID) {
            GUILayout.BeginVertical();
            GUILayout.Label ("Server status: " + (server.Running ? "Online" : "Offline"), labelStyle);
            if (server.Running) {
                if (GUILayout.Button ("Stop server"))
                    OnStopServerPressed (this, EventArgs.Empty);
                TCPServer tcpServer = (TCPServer)server.Server;

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Server address:", labelStyle);
                GUILayout.Label (tcpServer.Address.ToString (), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Server port:", labelStyle);
                GUILayout.Label (tcpServer.Port.ToString (), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Allowed client(s):", labelStyle);
                GUILayout.Label (AllowedClientsString(tcpServer.Address), labelStyle);
                GUILayout.EndHorizontal();

                if (server.Clients.Count () == 0) {
                    GUILayout.Label ("No clients connected", labelStyle);
                } else {
                    GUILayout.Label ("Clients connected:", labelStyle);
                    foreach (var client in server.Clients) {
                        string name = (client.Name == "") ? "<unknown>" : client.Name;
                        GUILayout.BeginHorizontal ();
                        GUILayout.Toggle (IsClientActive(client), "", activityStyle);
                        GUILayout.Label (name + " @ " + client.Address);
                        GUILayout.EndHorizontal ();
                    }
                }
            } else {
                if (GUILayout.Button ("Start server"))
                    OnStartServerPressed (this, EventArgs.Empty);
            }
            GUILayout.EndVertical ();
            UnityEngine.GUI.DragWindow ();
        }

        public void SawClientActivity (IClient client)
        {
            lastClientActivity [client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private bool IsClientActive (IClient client) {
            if (!lastClientActivity.ContainsKey (client))
                return false;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long lastActivity = lastClientActivity [client];
            return now - 100L < lastActivity;
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

