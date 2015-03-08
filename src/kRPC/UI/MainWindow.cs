using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using KRPC.Server;
using KRPC.Server.Net;

namespace KRPC.UI
{
    sealed class MainWindow : Window
    {
        public KRPCConfiguration Config { private get; set; }

        public KRPCServer Server { private get; set; }

        public ClientDisconnectDialog ClientDisconnectDialog { private get; set; }

        /// <summary>
        /// Errors to display
        /// </summary>
        public List<string> Errors { get; private set; }

        public event EventHandler OnStartServerPressed;
        public event EventHandler OnStopServerPressed;

        Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        const long lastActivityMillisecondsInterval = 100L;
        int numClientsDisplayed;
        // Editable fields
        string address;
        string rpcPort;
        string streamPort;
        // Style settings
        readonly Color errorColor = Color.yellow;
        GUIStyle labelStyle, stretchyLabelStyle, textFieldStyle, stretchyTextFieldStyle, buttonStyle,
            toggleStyle, separatorStyle, lightStyle, errorLabelStyle;
        const float windowWidth = 288f;
        const float addressWidth = 106f;
        const int addressMaxLength = 15;
        const float portWidth = 45f;
        const int portMaxLength = 5;
        // Text strings
        const string startButtonText = "Start server";
        const string stopButtonText = "Stop server";
        const string serverOnlineText = "Server online";
        const string serverOfflineText = "Server offline";
        const string addressLabelText = "Address:";
        const string rpcPortLabelText = "RPC port:";
        const string streamPortLabelText = "Stream port:";
        const string autoStartServerText = "Auto-start server";
        const string autoAcceptConnectionsText = "Auto-accept new clients";
        const string noClientsConnectedText = "No clients connected";
        const string unknownClientNameText = "<unknown>";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidRPCPortText = "RPC port must be between 0 and 65535";
        const string invalidStreamPortText = "Stream port must be between 0 and 65535";
        const string localClientOnlyText = "Local clients only";
        const string subnetAllowedText = "Subnet {0}";
        const string unknownClientsAllowedText = "Unknown visibility";
        const string autoAcceptingConnectionsText = "auto-accepting new clients";
        const string stringSeparatorText = ", ";

        protected override void Init ()
        {
            Server.OnClientActivity += (s, e) => SawClientActivity (e.Client);

            Style.fixedWidth = windowWidth;

            labelStyle = new GUIStyle (GUI.skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyLabelStyle = new GUIStyle (GUI.skin.label);
            stretchyLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyLabelStyle.stretchWidth = true;

            textFieldStyle = new GUIStyle (GUI.skin.textField);
            textFieldStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyTextFieldStyle = new GUIStyle (GUI.skin.textField);
            stretchyTextFieldStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyTextFieldStyle.stretchWidth = true;

            buttonStyle = new GUIStyle (GUI.skin.button);
            buttonStyle.margin = new RectOffset (0, 0, 0, 0);

            toggleStyle = new GUIStyle (GUI.skin.toggle);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);
            toggleStyle.stretchWidth = false;
            toggleStyle.contentOffset = new Vector2 (4, 0);

            separatorStyle = GUILayoutExtensions.SeparatorStyle (new Color (0f, 0f, 0f, 0.25f));
            separatorStyle.fixedHeight = 2;
            separatorStyle.stretchWidth = true;
            separatorStyle.margin = new RectOffset (2, 2, 3, 3);

            lightStyle = GUILayoutExtensions.LightStyle ();

            errorLabelStyle = new GUIStyle (GUI.skin.label);
            errorLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            errorLabelStyle.stretchWidth = true;
            errorLabelStyle.normal.textColor = errorColor;

            Errors = new List<string> ();
            address = Config.Address.ToString ();
            rpcPort = Config.RPCPort.ToString ();
            streamPort = Config.StreamPort.ToString ();
        }

        private void DrawServerStatus ()
        {
            GUILayoutExtensions.Light (Server.Running, lightStyle);
            if (Server.Running)
                GUILayout.Label (serverOnlineText, stretchyLabelStyle);
            else
                GUILayout.Label (serverOfflineText, stretchyLabelStyle);
        }

        private void DrawStartStopButton ()
        {
            if (Server.Running) {
                if (GUILayout.Button (stopButtonText, buttonStyle)) {
                    if (OnStopServerPressed != null)
                        OnStopServerPressed (this, EventArgs.Empty);
                    // Force window to resize to height of content
                    // TODO: better way to do this?
                    Position = new Rect (Position.x, Position.y, Position.width, 0f);
                }
            } else {
                if (GUILayout.Button (startButtonText, buttonStyle)) {
                    if (StartServer () && OnStartServerPressed != null)
                        OnStartServerPressed (this, EventArgs.Empty);
                }
            }
        }

        private void DrawAddress ()
        {
            if (Server.Running)
                GUILayout.Label (addressLabelText + " " + Server.Address, labelStyle);
            else {
                GUILayout.Label (addressLabelText, labelStyle);
                textFieldStyle.fixedWidth = addressWidth;
                address = GUILayout.TextField (address, addressMaxLength, stretchyTextFieldStyle);
            }
        }

        private void DrawRPCPort ()
        {
            if (Server.Running)
                GUILayout.Label (rpcPortLabelText + " " + Server.RPCPort, labelStyle);
            else {
                GUILayout.Label (rpcPortLabelText, labelStyle);
                textFieldStyle.fixedWidth = portWidth;
                rpcPort = GUILayout.TextField (rpcPort, portMaxLength, textFieldStyle);
            }
        }

        private void DrawStreamPort ()
        {
            if (Server.Running)
                GUILayout.Label (streamPortLabelText + " " + Server.StreamPort, labelStyle);
            else {
                GUILayout.Label (streamPortLabelText, labelStyle);
                textFieldStyle.fixedWidth = portWidth;
                streamPort = GUILayout.TextField (streamPort, portMaxLength, textFieldStyle);
            }
        }

        private void DrawAutoStartServerToggle ()
        {
            bool autoStartServer = GUILayout.Toggle (Config.AutoStartServer, autoStartServerText, toggleStyle, new GUILayoutOption[] { });
            if (autoStartServer != Config.AutoStartServer) {
                Config.AutoStartServer = autoStartServer;
                Config.Save ();
            }
        }

        private void DrawAutoAcceptConnectionsToggle ()
        {
            bool autoAcceptConnections = GUILayout.Toggle (Config.AutoAcceptConnections, autoAcceptConnectionsText, toggleStyle, new GUILayoutOption[] { });
            if (autoAcceptConnections != Config.AutoAcceptConnections) {
                Config.AutoAcceptConnections = autoAcceptConnections;
                Config.Save ();
            }
        }

        private void DrawServerInfo ()
        {
            string info = AllowedClientsString (Server.Address);
            if (Config.AutoAcceptConnections)
                info = info + stringSeparatorText + autoAcceptingConnectionsText;
            GUILayout.Label (info, labelStyle);
        }

        private void DrawClientsList ()
        {
            // Get list of client descriptions
            IDictionary<IClient,string> clientDescriptions = new Dictionary<IClient,string> ();
            if (Server.Clients.Any ()) {
                foreach (var client in Server.Clients) {
                    try {
                        string clientName = (client.Name == "") ? unknownClientNameText : client.Name;
                        clientDescriptions [client] = clientName + " @ " + client.Address;
                    } catch (ClientDisconnectedException) {
                    }
                }
            }

            // Display the list of clients
            if (clientDescriptions.Any ()) {
                foreach (var entry in clientDescriptions) {
                    var client = entry.Key;
                    var description = entry.Value;
                    GUILayout.BeginHorizontal ();
                    GUILayoutExtensions.Light (IsClientActive (client), lightStyle);
                    GUILayout.Label (description, stretchyLabelStyle);
                    if (GUILayout.Button (new GUIContent (Icons.Instance.buttonDisconnectClient, "Disconnect client"),
                            buttonStyle, GUILayout.MaxWidth (20), GUILayout.MaxHeight (20))) {
                        ClientDisconnectDialog.Show (client);
                    }
                    GUILayout.EndHorizontal ();
                }
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label (noClientsConnectedText, labelStyle);
                GUILayout.EndHorizontal ();
            }
        }

        protected override void Draw ()
        {
            // Force window to resize to height of content when length of client list changes
            // TODO: better way to do this?
            if (Server.Clients.Count () != numClientsDisplayed) {
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
                numClientsDisplayed = Server.Clients.Count ();
            }

            GUILayout.BeginVertical ();

            GUILayout.BeginHorizontal ();
            DrawServerStatus ();
            DrawStartStopButton ();
            GUILayout.EndHorizontal ();

            GUILayout.Space (4);

            GUILayout.BeginHorizontal ();
            DrawAddress ();
            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();
            DrawRPCPort ();
            GUILayout.Space (4);
            DrawStreamPort ();
            GUILayout.EndHorizontal ();

            if (Server.Running) {
                GUILayout.BeginHorizontal ();
                DrawServerInfo ();
                GUILayout.EndHorizontal ();

                GUILayoutExtensions.Separator (separatorStyle);

                DrawClientsList ();
            } else {
                GUILayout.BeginHorizontal ();
                DrawAutoStartServerToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                DrawAutoAcceptConnectionsToggle ();
                GUILayout.EndHorizontal ();

                foreach (var error in Errors)
                    GUILayout.Label (error, errorLabelStyle);
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        bool StartServer ()
        {
            // Validate the settings
            Errors.Clear ();
            IPAddress ignoreAddress;
            ushort ignorePort;
            bool validAddress = IPAddress.TryParse (address, out ignoreAddress);
            bool validRPCPort = UInt16.TryParse (rpcPort, out ignorePort);
            bool validStreamPort = UInt16.TryParse (streamPort, out ignorePort);

            // Display error message if required
            if (!validAddress)
                Errors.Add (invalidAddressText);
            if (!validRPCPort)
                Errors.Add (invalidRPCPortText);
            if (!validStreamPort)
                Errors.Add (invalidStreamPortText);

            // Save the settings and trigger start server event
            if (Errors.Count == 0) {
                Config.Load ();
                Config.Address = IPAddress.Parse (address);
                Config.RPCPort = Convert.ToUInt16 (rpcPort);
                Config.StreamPort = Convert.ToUInt16 (streamPort);
                Config.Save ();
                return true;
            }
            return false;
        }

        void SawClientActivity (IClient client)
        {
            lastClientActivity [client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        bool IsClientActive (IClient client)
        {
            if (!lastClientActivity.ContainsKey (client))
                return false;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long lastActivity = lastClientActivity [client];
            return now - lastActivityMillisecondsInterval < lastActivity;
        }

        string AllowedClientsString (IPAddress localAddress)
        {
            if (IPAddress.IsLoopback (localAddress))
                return localClientOnlyText;
            try {
                var subnet = NetworkInformation.GetSubnetMask (localAddress);
                return String.Format (subnetAllowedText, subnet);
            } catch (ArgumentException) {
            } catch (DllNotFoundException) {
            }
            return unknownClientsAllowedText;
        }
    }
}

