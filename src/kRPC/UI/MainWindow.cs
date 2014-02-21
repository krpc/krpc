using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using KRPC.Server;

namespace KRPC.UI
{
    sealed class MainWindow : Window
    {
        public KRPCConfiguration Config { private get; set; }
        public KRPCServer Server { private get; set; }

        public event EventHandler OnStartServerPressed;
        public event EventHandler OnStopServerPressed;

        private Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        private const long lastActivityInterval = 100L; // milliseconds

        // Remember number of clients displayed, to reset window height when it changes
        private int numClientsDisplayed = 0;

        // Editable fields
        private string address;
        private string port;

        // Errors to display
        public List<string> Errors { get; private set; }
        private readonly Color errorColor = Color.yellow;

        // Style settings
        private GUIStyle labelStyle, stretchyLabelStyle, textFieldStyle, buttonStyle, separatorStyle, lightStyle, errorLabelStyle;
        private const float windowWidth = 280f;
        private const float addressWidth = 106f;
        private const int addressMaxLength = 15;
        private const float portWidth = 45f;
        private const int portMaxLength = 5;

        // Strings
        private const string startButtonText = "Start server";
        private const string stopButtonText = "Stop server";
        private const string serverOnlineText = "Server online";
        private const string serverOfflineText = "Server offline";
        private const string addressLabelText = "Address:";
        private const string portLabelText = "Port:";
        private const string noClientsConnectedText = "No clients connected";
        private const string unknownClientNameText = "<unknown>";
        private const string invalidAddressText = "Invalid IP address. Must be in dot-decimal notation, e.g. \"192.168.1.0\"";
        private const string invalidPortText = "Port must be between 0 and 65535";

        private const string localClientOnlyText = "(Local clients only)";
        private const string subnetAllowedText = "(Subnet {0})";
        private const string unknownClientsAllowedText = "(Unknown visibility!)";

        protected override void Init() {
            Style.fixedWidth = windowWidth;

            labelStyle = new GUIStyle (UnityEngine.GUI.skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyLabelStyle = new GUIStyle (UnityEngine.GUI.skin.label);
            stretchyLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyLabelStyle.stretchWidth = true;

            textFieldStyle = new GUIStyle (UnityEngine.GUI.skin.textField);
            textFieldStyle.margin = new RectOffset (0, 0, 0, 0);

            buttonStyle = new GUIStyle (UnityEngine.GUI.skin.button);

            separatorStyle = GUILayoutExtensions.SeparatorStyle (new Color(0f, 0f, 0f, 0.25f));
            separatorStyle.fixedHeight = 2;
            separatorStyle.stretchWidth = true;
            separatorStyle.margin = new RectOffset (2, 2, 3, 3);

            lightStyle = GUILayoutExtensions.LightStyle ();

            errorLabelStyle = new GUIStyle (UnityEngine.GUI.skin.label);
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

                if (Server.Clients.Count () == 0) {
                    GUILayout.BeginHorizontal ();
                    GUILayout.Label (noClientsConnectedText, labelStyle);
                    GUILayout.EndHorizontal ();
                } else {
                    foreach (var client in Server.Clients) {
                        string name = (client.Name == "") ? unknownClientNameText : client.Name;
                        GUILayout.BeginHorizontal ();
                        GUILayoutExtensions.Light (IsClientActive (client), lightStyle);
                        GUILayout.Label (name + " @ " + client.Address, stretchyLabelStyle);
                        GUILayout.EndHorizontal ();
                    }
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

        private bool StartServer ()
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

        public void SawClientActivity (IClient client)
        {
            lastClientActivity [client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private bool IsClientActive (IClient client) {
            if (!lastClientActivity.ContainsKey (client))
                return false;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long lastActivity = lastClientActivity [client];
            return now - lastActivityInterval < lastActivity;
        }

        private string AllowedClientsString(IPAddress localAddress) {
            // TODO: better way of checking if address is the loopback device?
            if (localAddress.ToString () == IPAddress.Loopback.ToString ())
                return localClientOnlyText;
            var subnet = GetSubnetMask (localAddress);
            if (subnet != null)
                return String.Format (subnetAllowedText, subnet);
            return unknownClientsAllowedText;
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

