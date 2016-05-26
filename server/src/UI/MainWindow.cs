using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using KRPC.Server;
using KRPC.Server.TCP;
using UnityEngine;

namespace KRPC.UI
{
    sealed class MainWindow : Window
    {
        public KRPCConfiguration Config { private get; set; }

        public KRPCServer Server { private get; set; }

        public InfoWindow InfoWindow { private get; set; }

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
        bool resized;
        // Editable fields
        string address;
        bool manualAddress;
        List<string> availableAddresses;
        string rpcPort;
        string streamPort;
        bool advanced;
        string maxTimePerUpdate;
        string recvTimeout;
        // Style settings
        readonly Color errorColor = Color.yellow;
        GUIStyle labelStyle, stretchyLabelStyle, fixedLabelStyle, textFieldStyle, longTextFieldStyle, stretchyTextFieldStyle,
            buttonStyle, toggleStyle, separatorStyle, lightStyle, errorLabelStyle, comboOptionsStyle, comboOptionStyle;
        const float windowWidth = 288f;
        const float textFieldWidth = 45f;
        const float longTextFieldWidth = 90f;
        const float fixedLabelWidth = 125f;
        const float indentWidth = 15f;
        const int addressMaxLength = 15;
        const int portMaxLength = 5;
        const int maxTimePerUpdateMaxLength = 5;
        const int recvTimeoutMaxLength = 5;
        // Text strings
        const string title = "kRPC Server";
        const string startButtonText = "Start server";
        const string stopButtonText = "Stop server";
        const string serverOnlineText = "Server online";
        const string serverOfflineText = "Server offline";
        const string addressLabelText = "Address:";
        const string rpcPortLabelText = "RPC port:";
        const string streamPortLabelText = "Stream port:";
        const string localhostText = "localhost";
        const string manualText = "Manual";
        const string anyText = "Any";
        const string showInfoWindowText = "Show Info";
        const string advancedText = "Advanced settings";
        const string autoStartServerText = "Auto-start server";
        const string autoAcceptConnectionsText = "Auto-accept new clients";
        const string oneRPCPerUpdateText = "One RPC per update";
        const string maxTimePerUpdateText = "Max. time per update";
        const string adaptiveRateControlText = "Adaptive rate control";
        const string blockingRecvText = "Blocking receives";
        const string recvTimeoutText = "Receive timeout";
        const string noClientsConnectedText = "No clients connected";
        const string unknownClientNameText = "<unknown>";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidRPCPortText = "RPC port must be between 0 and 65535";
        const string invalidStreamPortText = "Stream port must be between 0 and 65535";
        const string invalidMaxTimePerUpdateText = "Max. time per update must be an integer";
        const string invalidRecvTimeoutText = "Receive timeout must be an integer";
        const string localClientOnlyText = "Local clients only";
        const string anyClientText = "Any client";
        const string subnetAllowedText = "Subnet {0}";
        const string unknownClientsAllowedText = "Unknown visibility";
        const string autoAcceptingConnectionsText = "auto-accepting new clients";
        const string stringSeparatorText = ", ";

        protected override void Init ()
        {
            Title = title;

            Server.OnClientActivity += (s, e) => SawClientActivity (e.Client);

            Style.fixedWidth = windowWidth;

            var skin = Skin.DefaultSkin;

            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyLabelStyle = new GUIStyle (skin.label);
            stretchyLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyLabelStyle.stretchWidth = true;

            fixedLabelStyle = new GUIStyle (skin.label);
            fixedLabelStyle.fixedWidth = fixedLabelWidth;

            textFieldStyle = new GUIStyle (skin.textField);
            textFieldStyle.margin = new RectOffset (0, 0, 0, 0);
            textFieldStyle.fixedWidth = textFieldWidth;

            longTextFieldStyle = new GUIStyle (skin.textField);
            longTextFieldStyle.margin = new RectOffset (0, 0, 0, 0);
            longTextFieldStyle.fixedWidth = longTextFieldWidth;

            stretchyTextFieldStyle = new GUIStyle (skin.textField);
            stretchyTextFieldStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyTextFieldStyle.stretchWidth = true;

            buttonStyle = new GUIStyle (skin.button);
            buttonStyle.margin = new RectOffset (0, 0, 0, 0);

            toggleStyle = new GUIStyle (skin.toggle);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);
            toggleStyle.stretchWidth = false;
            toggleStyle.contentOffset = new Vector2 (4, 0);

            separatorStyle = GUILayoutExtensions.SeparatorStyle (new Color (0f, 0f, 0f, 0.25f));
            separatorStyle.fixedHeight = 2;
            separatorStyle.stretchWidth = true;
            separatorStyle.margin = new RectOffset (2, 2, 3, 3);

            lightStyle = GUILayoutExtensions.LightStyle ();

            errorLabelStyle = new GUIStyle (skin.label);
            errorLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            errorLabelStyle.stretchWidth = true;
            errorLabelStyle.normal.textColor = errorColor;

            comboOptionsStyle = GUILayoutExtensions.ComboOptionsStyle ();
            comboOptionStyle = GUILayoutExtensions.ComboOptionStyle ();

            Errors = new List<string> ();
            address = Config.Address.ToString ();
            rpcPort = Config.RPCPort.ToString ();
            streamPort = Config.StreamPort.ToString ();
            maxTimePerUpdate = Config.MaxTimePerUpdate.ToString ();
            recvTimeout = Config.RecvTimeout.ToString ();

            // Get list of available addresses for drop down
            var interfaceAddresses = NetworkInformation.GetLocalIPAddresses ().Select (x => x.ToString ()).ToList ();
            interfaceAddresses.Remove (IPAddress.Loopback.ToString ());
            interfaceAddresses.Remove (IPAddress.Any.ToString ());
            availableAddresses = new List<string> (new [] { localhostText, anyText });
            availableAddresses.AddRange (interfaceAddresses);
            availableAddresses.Add (manualText);
        }

        void DrawServerStatus ()
        {
            GUILayoutExtensions.Light (Server.Running, lightStyle);
            if (Server.Running)
                GUILayout.Label (serverOnlineText, stretchyLabelStyle);
            else
                GUILayout.Label (serverOfflineText, stretchyLabelStyle);
        }

        void DrawStartStopButton ()
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

        void DrawAddress ()
        {
            if (Server.Running)
                GUILayout.Label (addressLabelText + " " + Server.Address, labelStyle);
            else {
                GUILayout.Label (addressLabelText, labelStyle);
                // Get the index of the address in the combo box
                int selected;
                if (!manualAddress && address == IPAddress.Loopback.ToString ())
                    selected = 0;
                else if (!manualAddress && address == IPAddress.Any.ToString ())
                    selected = 1;
                else if (!manualAddress && availableAddresses.Contains (address))
                    selected = availableAddresses.IndexOf (address);
                else
                    selected = availableAddresses.Count () - 1;
                // Display the combo box
                selected = GUILayoutExtensions.ComboBox ("address", selected, availableAddresses, buttonStyle, comboOptionsStyle, comboOptionStyle);
                // Get the address from the combo box selection
                if (selected == 0) {
                    address = IPAddress.Loopback.ToString ();
                    manualAddress = false;
                } else if (selected == 1) {
                    address = IPAddress.Any.ToString ();
                    manualAddress = false;
                } else if (selected < availableAddresses.Count () - 1) {
                    address = availableAddresses [selected];
                    manualAddress = false;
                } else {
                    // Display a text field when "Manual" is selected
                    address = GUILayout.TextField (address, addressMaxLength, stretchyTextFieldStyle);
                    manualAddress = true;
                }
            }
        }

        void DrawShowInfoWindow ()
        {
            if (GUILayout.Button (showInfoWindowText, buttonStyle)) {
                InfoWindow.Visible = true;
            }
        }

        void DrawRPCPort ()
        {
            if (Server.Running)
                GUILayout.Label (rpcPortLabelText + " " + Server.RPCPort, labelStyle);
            else {
                GUILayout.Label (rpcPortLabelText, labelStyle);
                rpcPort = GUILayout.TextField (rpcPort, portMaxLength, textFieldStyle);
            }
        }

        void DrawStreamPort ()
        {
            if (Server.Running)
                GUILayout.Label (streamPortLabelText + " " + Server.StreamPort, labelStyle);
            else {
                GUILayout.Label (streamPortLabelText, labelStyle);
                streamPort = GUILayout.TextField (streamPort, portMaxLength, textFieldStyle);
            }
        }

        void DrawAdvancedToggle ()
        {
            bool newAdvanced = GUILayout.Toggle (advanced, advancedText, toggleStyle, new GUILayoutOption[] { });
            if (newAdvanced != advanced) {
                advanced = newAdvanced;
                resized = true;
            }
        }

        void DrawAutoStartServerToggle ()
        {
            bool autoStartServer = GUILayout.Toggle (Config.AutoStartServer, autoStartServerText, toggleStyle, new GUILayoutOption[] { });
            if (autoStartServer != Config.AutoStartServer) {
                Config.AutoStartServer = autoStartServer;
                Config.Save ();
            }
        }

        void DrawAutoAcceptConnectionsToggle ()
        {
            bool autoAcceptConnections = GUILayout.Toggle (Config.AutoAcceptConnections, autoAcceptConnectionsText, toggleStyle, new GUILayoutOption[] { });
            if (autoAcceptConnections != Config.AutoAcceptConnections) {
                Config.AutoAcceptConnections = autoAcceptConnections;
                Config.Save ();
            }
        }

        void DrawOneRPCPerUpdateToggle ()
        {
            bool oneRPCPerUpdate = GUILayout.Toggle (Config.OneRPCPerUpdate, oneRPCPerUpdateText, toggleStyle, new GUILayoutOption[] { });
            if (oneRPCPerUpdate != Config.OneRPCPerUpdate) {
                Config.OneRPCPerUpdate = oneRPCPerUpdate;
                Config.Save ();
            }
        }

        void DrawMaxTimePerUpdate ()
        {
            GUILayout.Label (maxTimePerUpdateText, fixedLabelStyle);
            maxTimePerUpdate = GUILayout.TextField (maxTimePerUpdate, maxTimePerUpdateMaxLength, longTextFieldStyle);
        }

        void DrawAdaptiveRateControlToggle ()
        {
            bool adaptiveRateControl = GUILayout.Toggle (Config.AdaptiveRateControl, adaptiveRateControlText, toggleStyle, new GUILayoutOption[] { });
            if (adaptiveRateControl != Config.AdaptiveRateControl) {
                Config.AdaptiveRateControl = adaptiveRateControl;
                Config.Save ();
            }
        }

        void DrawBlockingRecvToggle ()
        {
            bool blockingUpdate = GUILayout.Toggle (Config.BlockingRecv, blockingRecvText, toggleStyle, new GUILayoutOption[] { });
            if (blockingUpdate != Config.BlockingRecv) {
                Config.BlockingRecv = blockingUpdate;
                Config.Save ();
            }
        }

        void DrawRecvTimeout ()
        {
            GUILayout.Label (recvTimeoutText, fixedLabelStyle);
            recvTimeout = GUILayout.TextField (recvTimeout, recvTimeoutMaxLength, longTextFieldStyle);
        }

        void DrawServerInfo ()
        {
            string info = AllowedClientsString (Server.Address);
            if (Config.AutoAcceptConnections)
                info = info + stringSeparatorText + autoAcceptingConnectionsText;
            GUILayout.Label (info, labelStyle);
        }

        void DrawClientsList ()
        {
            // Resize window if number of connected clients changes
            if (Server.Clients.Count () != numClientsDisplayed) {
                numClientsDisplayed = Server.Clients.Count ();
                resized = true;
            }
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
            // Force window to resize to height of content
            if (resized) {
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
                resized = false;
            }

            GUILayout.BeginVertical ();

            GUILayout.BeginHorizontal ();
            DrawServerStatus ();
            DrawStartStopButton ();
            GUILayout.EndHorizontal ();

            GUILayout.Space (4);

            GUILayout.BeginHorizontal ();
            DrawAddress ();
            if (Server.Running) {
                GUILayout.Space (4);
                DrawShowInfoWindow ();
            }
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
                DrawAdvancedToggle ();
                GUILayout.EndHorizontal ();

                if (advanced) {
                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawAutoStartServerToggle ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawAutoAcceptConnectionsToggle ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawOneRPCPerUpdateToggle ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawMaxTimePerUpdate ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawAdaptiveRateControlToggle ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawBlockingRecvToggle ();
                    GUILayout.EndHorizontal ();

                    GUILayout.BeginHorizontal ();
                    GUILayout.Space (indentWidth);
                    DrawRecvTimeout ();
                    GUILayout.EndHorizontal ();
                }

                foreach (var error in Errors)
                    GUILayout.Label (error, errorLabelStyle);
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        bool StartServer ()
        {
            // Resize the window to the contents
            resized = true;

            // Validate the settings
            Errors.Clear ();
            IPAddress ignoreAddress;
            ushort ignoreInt;
            bool validAddress = IPAddress.TryParse (address, out ignoreAddress);
            bool validRPCPort = UInt16.TryParse (rpcPort, out ignoreInt);
            bool validStreamPort = UInt16.TryParse (streamPort, out ignoreInt);
            bool validMaxTimePerUpdate = UInt16.TryParse (maxTimePerUpdate, out ignoreInt);
            bool validRecvTimeout = UInt16.TryParse (recvTimeout, out ignoreInt);

            // Display error message if required
            if (!validAddress)
                Errors.Add (invalidAddressText);
            if (!validRPCPort)
                Errors.Add (invalidRPCPortText);
            if (!validStreamPort)
                Errors.Add (invalidStreamPortText);
            if (!validMaxTimePerUpdate)
                Errors.Add (invalidMaxTimePerUpdateText);
            if (!validRecvTimeout)
                Errors.Add (invalidRecvTimeoutText);

            // Save the settings and trigger start server event
            if (Errors.Count == 0) {
                Config.Load ();
                Config.Address = IPAddress.Parse (address);
                Config.RPCPort = Convert.ToUInt16 (rpcPort);
                Config.StreamPort = Convert.ToUInt16 (streamPort);
                Config.MaxTimePerUpdate = Convert.ToUInt16 (maxTimePerUpdate);
                Config.RecvTimeout = Convert.ToUInt16 (recvTimeout);
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

        static string AllowedClientsString (IPAddress localAddress)
        {
            if (IPAddress.IsLoopback (localAddress))
                return localClientOnlyText;
            else if (localAddress == IPAddress.Any)
                return anyClientText;
            try {
                var subnet = NetworkInformation.GetSubnetMask (localAddress);
                return String.Format (subnetAllowedText, subnet);
            } catch (NotImplementedException) {
            } catch (ArgumentException) {
            } catch (DllNotFoundException) {
            }
            return unknownClientsAllowedText;
        }
    }
}

