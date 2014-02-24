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
        string port;
        readonly Color errorColor = Color.yellow;
        // Style settings
        GUIStyle labelStyle, stretchyLabelStyle, textFieldStyle, buttonStyle,
            separatorStyle, lightStyle, errorLabelStyle;
        const float windowWidth = 280f;
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
        const string portLabelText = "Port:";
        const string noClientsConnectedText = "No clients connected";
        const string unknownClientNameText = "<unknown>";
        const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        const string invalidPortText = "Port must be between 0 and 65535";
        const string localClientOnlyText = "(Local clients only)";
        const string subnetAllowedText = "(Subnet {0})";
        const string unknownClientsAllowedText = "(Unknown visibility!)";

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

            buttonStyle = new GUIStyle (GUI.skin.button);

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
            port = Config.Port.ToString ();
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
            if (Server.Running) {

                if (GUILayout.Button (stopButtonText, buttonStyle)) {
                    if (OnStopServerPressed != null)
                        OnStopServerPressed (this, EventArgs.Empty);
                    // Force window to resize to height of content
                    // TODO: better way to do this?
                    Position = new Rect (Position.x, Position.y, Position.width, 0f);
                }

                GUILayout.BeginHorizontal ();
                GUILayoutExtensions.Light (true, lightStyle);
                GUILayout.Label (serverOnlineText, stretchyLabelStyle);
                GUILayout.Label (AllowedClientsString (Server.Address), labelStyle);
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Label (addressLabelText, labelStyle);
                GUILayout.Label (Server.Address.ToString (), stretchyLabelStyle);
                GUILayout.Label (portLabelText, labelStyle);
                GUILayout.Label (Server.Port.ToString (), stretchyLabelStyle);
                GUILayout.EndHorizontal ();

                GUILayoutExtensions.Separator (separatorStyle);

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
                            client.Close ();
                        }
                        GUILayout.EndHorizontal ();
                    }
                } else {
                    GUILayout.BeginHorizontal ();
                    GUILayout.Label (noClientsConnectedText, labelStyle);
                    GUILayout.EndHorizontal ();
                }
            } else {
                if (GUILayout.Button (startButtonText, buttonStyle)) {
                    if (StartServer () && OnStartServerPressed != null)
                        OnStartServerPressed (this, EventArgs.Empty);
                }

                GUILayout.BeginHorizontal ();
                GUILayoutExtensions.Light (false, lightStyle);
                GUILayout.Label (serverOfflineText, stretchyLabelStyle);
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Label (addressLabelText, stretchyLabelStyle);
                textFieldStyle.fixedWidth = addressWidth;
                address = GUILayout.TextField (address, addressMaxLength, textFieldStyle);
                GUILayout.Label (portLabelText, stretchyLabelStyle);
                textFieldStyle.fixedWidth = portWidth;
                port = GUILayout.TextField (port, portMaxLength, textFieldStyle);
                GUILayout.EndHorizontal ();

                foreach (var error in Errors) {
                    GUILayout.Label (error, errorLabelStyle);
                }
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
            bool validPort = UInt16.TryParse (port, out ignorePort);

            // Display error message if required
            if (!validAddress)
                Errors.Add (invalidAddressText);
            if (!validPort)
                Errors.Add (invalidPortText);

            // Save the settings and trigger start server event
            if (Errors.Count == 0) {
                Config.Port = Convert.ToUInt16 (port);
                Config.Address = IPAddress.Parse (address);
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
            // TODO: better way of checking if address is the loopback device?
            if (localAddress.ToString () == IPAddress.Loopback.ToString ())
                return localClientOnlyText;
            try {
                var subnet = NetworkInformation.GetSubnetMask (localAddress);
                return String.Format (subnetAllowedText, subnet);
            } catch (ArgumentException) {
            }
            return unknownClientsAllowedText;
        }
    }
}

