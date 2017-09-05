using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using KRPC.Server;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.UI
{
    sealed class MainWindow : Window
    {
        Core core;
        ConfigurationFile config;

        public InfoWindow InfoWindow { private get; set; }

        public ClientDisconnectDialog ClientDisconnectDialog { get; set; }

        public List<string> Errors { get; private set; }

        public event EventHandler<ServerEventArgs> OnStartServerPressed;
        public event EventHandler<ServerEventArgs> OnStopServerPressed;

        readonly HashSet<Guid> expandServers = new HashSet<Guid> ();
        readonly IDictionary<Guid, EditServer> editServers = new Dictionary<Guid, EditServer> ();
        readonly IDictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        const long lastActivityMillisecondsInterval = 100;

        internal bool Resized { get; set; }

        bool showAdvancedOptions;
        string maxTimePerUpdate;
        string recvTimeout;
        // Style settings
        readonly Color errorColor = Color.yellow;
        internal GUIStyle labelStyle, stretchyLabelStyle, fixedLabelStyle, textFieldStyle, longTextFieldStyle, stretchyTextFieldStyle,
            buttonStyle, toggleStyle, expandStyle, separatorStyle, lightStyle, errorLabelStyle, comboOptionsStyle, comboOptionStyle;
        const float windowWidth = 288f;
        const float textFieldWidth = 45f;
        const float longTextFieldWidth = 90f;
        const float fixedLabelWidth = 125f;
        const float indentWidth = 15f;
        float scaledIndentWidth;
        const int maxTimePerUpdateMaxLength = 5;
        const int recvTimeoutMaxLength = 5;
        // Text strings
        const string addServerText = "Add Server";
        const string removeServerText = "Remove";
        const string startServerText = "Start";
        const string stopServerText = "Stop";
        const string editServerText = "Edit";
        const string saveServerText = "Save";
        const string serverOnlineText = "Server online";
        const string serverOfflineText = "Server offline";
        internal const string protobufOverTcpText = "Protobuf over TCP";
        internal const string protobufOverWebSocketsText = "Protobuf over WebSockets";
        const string unknownClientNameText = "<unknown>";
        const string noClientsConnectedText = "No clients connected";
        const string advancedText = "Advanced settings";
        const string autoStartServerText = "Auto-start server";
        const string autoAcceptConnectionsText = "Auto-accept new clients";
        const string confirmRemoveClientText = "Confirm disconnecting a client";
        const string oneRPCPerUpdateText = "One RPC per update";
        const string maxTimePerUpdateText = "Max. time per update";
        const string adaptiveRateControlText = "Adaptive rate control";
        const string blockingRecvText = "Blocking receives";
        const string recvTimeoutText = "Receive timeout";
        const string debugLoggingText = "Debug logging";
        const string invalidMaxTimePerUpdateText = "Max. time per update must be an integer";
        const string invalidRecvTimeoutText = "Receive timeout must be an integer";
        const string showInfoWindowText = "Show info";

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        protected override void Init ()
        {
            core = Core.Instance;
            config = ConfigurationFile.Instance;

            var version = FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);
            Title = "kRPC v" + version.FileMajorPart + "." + version.FileMinorPart + "." + version.FileBuildPart;

            core.OnClientActivity += (s, e) => SawClientActivity (e.Client);

            Style.fixedWidth = windowWidth;

            var skin = Skin.DefaultSkin;

            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyLabelStyle = new GUIStyle (skin.label);
            stretchyLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyLabelStyle.stretchWidth = true;

            fixedLabelStyle = new GUIStyle (skin.label);

            textFieldStyle = new GUIStyle (skin.textField);
            textFieldStyle.margin = new RectOffset (0, 0, 0, 0);

            longTextFieldStyle = new GUIStyle (skin.textField);
            longTextFieldStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyTextFieldStyle = new GUIStyle (skin.textField);
            stretchyTextFieldStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyTextFieldStyle.stretchWidth = true;

            buttonStyle = new GUIStyle (skin.button);
            buttonStyle.margin = new RectOffset (0, 0, 0, 0);

            toggleStyle = new GUIStyle (skin.toggle);
            toggleStyle.margin = new RectOffset (0, 0, 0, 0);
            toggleStyle.stretchWidth = false;
            toggleStyle.contentOffset = new Vector2 (4, 0);

            expandStyle = new GUIStyle (skin.button);
            expandStyle.margin = new RectOffset (0, 0, 0, 0);
            expandStyle.padding = new RectOffset (0, 0, 0, 0);
            expandStyle.fixedWidth = 16;
            expandStyle.fixedHeight = 16;

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
            maxTimePerUpdate = config.Configuration.MaxTimePerUpdate.ToString ();
            recvTimeout = config.Configuration.RecvTimeout.ToString ();

            core.OnClientActivity += (s, e) => SawClientActivity (e.Client);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        protected override void Draw (bool needRescale)
        {
            if (needRescale) {
                int scaledFontSize = Style.fontSize;
                scaledIndentWidth = indentWidth * GameSettings.UI_SCALE;
                Style.fixedWidth = windowWidth * GameSettings.UI_SCALE;

                labelStyle.fontSize = scaledFontSize;
                stretchyLabelStyle.fontSize = scaledFontSize;
                fixedLabelStyle.fontSize = scaledFontSize;
                fixedLabelStyle.fixedWidth = fixedLabelWidth * GameSettings.UI_SCALE;
                textFieldStyle.fontSize = scaledFontSize;
                textFieldStyle.fixedWidth = textFieldWidth * GameSettings.UI_SCALE;
                longTextFieldStyle.fontSize = scaledFontSize;
                longTextFieldStyle.fixedWidth = longTextFieldWidth * GameSettings.UI_SCALE;
                stretchyTextFieldStyle.fontSize = scaledFontSize;
                buttonStyle.fontSize = scaledFontSize;
                toggleStyle.fontSize = scaledFontSize;
                separatorStyle.fontSize = scaledFontSize;
                lightStyle.fontSize = scaledFontSize;
                errorLabelStyle.fontSize = scaledFontSize;
                comboOptionsStyle.fontSize = scaledFontSize;
                GUILayoutExtensions.SetLightStyleSize (lightStyle, Style.lineHeight);
                Resized = true;
            }

            // Force window to resize to height of content
            if (Resized) {
                Position = new Rect (Position.x, Position.y, Position.width, 0f);
                Resized = false;
            }

            GUILayout.BeginVertical ();
            foreach (var server in core.Servers.ToList()) {
                DrawServer (server);
                GUILayoutExtensions.Separator (separatorStyle);
            }

            DrawAddServer ();
            GUILayoutExtensions.Separator (separatorStyle);

            if (Errors.Any ()) {
                foreach (var error in Errors)
                    GUILayout.Label (error, errorLabelStyle);
                GUILayoutExtensions.Separator (separatorStyle);
            }

            DrawAdvancedOptions ();
            DrawShowInfoWindow ();

            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        void DrawServer (Server.Server server)
        {
            bool running = server.Running;
            bool editingServer = editServers.ContainsKey (server.Id);
            bool expanded = expandServers.Contains (server.Id);

            GUILayout.BeginHorizontal ();
            var icons = Icons.Instance;
            if (GUILayout.Button (new GUIContent (expanded ? icons.ButtonCollapse : icons.ButtonExpand, expanded ? "Collapse" : "Expand"),
                    expandStyle, GUILayout.MaxWidth (20), GUILayout.MaxHeight (20))) {
                if (expanded)
                    expandServers.Remove (server.Id);
                else
                    expandServers.Add (server.Id);
                expanded = !expanded;
                Resized = true;
            }
            GUILayoutExtensions.Light (running, lightStyle);
            if (!editingServer)
                GUILayout.Label (server.Name, labelStyle);
            else
                editServers [server.Id].DrawName ();
            GUILayout.EndHorizontal ();

            if (editingServer) {
                editServers [server.Id].Draw ();
            } else if (expanded) {
                GUILayout.Label (server.Protocol == Protocol.ProtocolBuffersOverTCP ? protobufOverTcpText : protobufOverWebSocketsText, labelStyle);
                GUILayout.Label (server.Info, labelStyle);
                foreach (var line in server.Address.Split ('\n'))
                    GUILayout.Label (line, labelStyle);
                DrawClients (server);
            }

            GUILayout.BeginHorizontal ();
            GUI.enabled = !editingServer;
            if (GUILayout.Button (running ? stopServerText : startServerText, buttonStyle))
                EventHandlerExtensions.Invoke (running ? OnStopServerPressed : OnStartServerPressed, this, new ServerEventArgs (server));
            GUI.enabled = !running;
            if (GUILayout.Button (editingServer ? saveServerText : editServerText, buttonStyle)) {
                if (editingServer) {
                    var newServer = editServers [server.Id].Save ();
                    if (newServer != null) {
                        editServers.Remove (server.Id);
                        config.Configuration.ReplaceServer (newServer);
                        config.Save ();
                        core.Replace (newServer.Create ());
                    }
                } else {
                    editServers [server.Id] = new EditServer (this, config.Configuration.GetServer (server.Id));
                }
                Resized = true;
            }
            GUI.enabled = !editingServer && !running;
            if (GUILayout.Button (removeServerText, buttonStyle)) {
                config.Configuration.RemoveServer (server.Id);
                config.Save ();
                core.Remove (server.Id);
                Resized = true;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal ();
        }

        void DrawClients (IServer server)
        {
            var clients = server.Clients.ToList ();
            IDictionary<IClient,string> clientDescriptions = new Dictionary<IClient,string> ();
            if (clients.Count > 0) {
                foreach (var client in clients) {
                    try {
                        var clientName = client.Name;
                        clientDescriptions [client] = (clientName.Length == 0 ? unknownClientNameText : clientName) + " @ " + client.Address;
                    } catch (ClientDisconnectedException) {
                    }
                }
            }

            if (clientDescriptions.Any ()) {
                foreach (var entry in clientDescriptions) {
                    var client = entry.Key;
                    var description = entry.Value;
                    GUILayout.BeginHorizontal ();
                    GUILayoutExtensions.Light (IsClientActive (client), lightStyle);
                    GUILayout.Label (description, stretchyLabelStyle);
                    if (GUILayout.Button (new GUIContent (Icons.Instance.ButtonDisconnectClient, "Disconnect client"),
                            buttonStyle, GUILayout.MaxWidth (20), GUILayout.MaxHeight (20))) {
                        if (config.Configuration.ConfirmRemoveClient)
                            ClientDisconnectDialog.Show (client);
                        else
                            client.Close ();
                    }
                    GUILayout.EndHorizontal ();
                }
            } else {
                GUILayout.BeginHorizontal ();
                GUILayout.Label (noClientsConnectedText, labelStyle);
                GUILayout.EndHorizontal ();
            }
        }

        void DrawAddServer ()
        {
            if (GUILayout.Button (addServerText, buttonStyle)) {
                var server = new Configuration.Server ();
                config.Configuration.Servers.Add (server);
                config.Save ();
                core.Add (server.Create ());
            }
        }

        void DrawAdvancedOptions ()
        {
            GUILayout.BeginHorizontal ();
            DrawAdvancedOptionsToggle ();
            GUILayout.EndHorizontal ();

            if (showAdvancedOptions) {
                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawAutoStartServerToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawAutoAcceptConnectionsToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawConfirmRemoveClientToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawOneRPCPerUpdateToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawMaxTimePerUpdate ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawAdaptiveRateControlToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawBlockingRecvToggle ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawRecvTimeout ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawDebugLogging ();
                GUILayout.EndHorizontal ();
            }
        }

        void DrawAdvancedOptionsToggle ()
        {
            bool value = GUILayout.Toggle (showAdvancedOptions, advancedText, toggleStyle, new GUILayoutOption[] { });
            if (value != showAdvancedOptions) {
                showAdvancedOptions = value;
                Resized = true;
            }
        }

        void DrawAutoStartServerToggle ()
        {
            bool autoStartServers = GUILayout.Toggle (config.Configuration.AutoStartServers, autoStartServerText, toggleStyle, new GUILayoutOption[] { });
            if (autoStartServers != config.Configuration.AutoStartServers) {
                config.Configuration.AutoStartServers = autoStartServers;
                config.Save ();
            }
        }

        void DrawAutoAcceptConnectionsToggle ()
        {
            bool autoAcceptConnections = GUILayout.Toggle (config.Configuration.AutoAcceptConnections, autoAcceptConnectionsText, toggleStyle, new GUILayoutOption[] { });
            if (autoAcceptConnections != config.Configuration.AutoAcceptConnections) {
                config.Configuration.AutoAcceptConnections = autoAcceptConnections;
                config.Save ();
            }
        }

        void DrawConfirmRemoveClientToggle ()
        {
            bool confirmRemoveClient = GUILayout.Toggle (config.Configuration.ConfirmRemoveClient, confirmRemoveClientText, toggleStyle, new GUILayoutOption[] { });
            if (confirmRemoveClient != config.Configuration.ConfirmRemoveClient) {
                config.Configuration.ConfirmRemoveClient = confirmRemoveClient;
                config.Save ();
            }
        }

        void DrawOneRPCPerUpdateToggle ()
        {
            bool oneRPCPerUpdate = GUILayout.Toggle (config.Configuration.OneRPCPerUpdate, oneRPCPerUpdateText, toggleStyle, new GUILayoutOption[] { });
            if (oneRPCPerUpdate != config.Configuration.OneRPCPerUpdate) {
                config.Configuration.OneRPCPerUpdate = oneRPCPerUpdate;
                config.Save ();
            }
        }

        void DrawMaxTimePerUpdate ()
        {
            GUILayout.Label (maxTimePerUpdateText, fixedLabelStyle);
            var newMaxTimePerUpdate = GUILayout.TextField (maxTimePerUpdate, maxTimePerUpdateMaxLength, longTextFieldStyle);
            if (newMaxTimePerUpdate != maxTimePerUpdate) {
                uint value = config.Configuration.MaxTimePerUpdate;
                uint.TryParse (newMaxTimePerUpdate, out value);
                config.Configuration.MaxTimePerUpdate = value;
                config.Save ();
            }
        }

        void DrawAdaptiveRateControlToggle ()
        {
            bool adaptiveRateControl = GUILayout.Toggle (config.Configuration.AdaptiveRateControl, adaptiveRateControlText, toggleStyle, new GUILayoutOption[] { });
            if (adaptiveRateControl != config.Configuration.AdaptiveRateControl) {
                config.Configuration.AdaptiveRateControl = adaptiveRateControl;
                config.Save ();
            }
        }

        void DrawBlockingRecvToggle ()
        {
            bool blockingRecv = GUILayout.Toggle (config.Configuration.BlockingRecv, blockingRecvText, toggleStyle, new GUILayoutOption[] { });
            if (blockingRecv != config.Configuration.BlockingRecv) {
                config.Configuration.BlockingRecv = blockingRecv;
                config.Save ();
            }
        }

        void DrawRecvTimeout ()
        {
            GUILayout.Label (recvTimeoutText, fixedLabelStyle);
            var newRecvTimeout = GUILayout.TextField (recvTimeout, recvTimeoutMaxLength, longTextFieldStyle);
            if (newRecvTimeout != recvTimeout) {
                uint value = config.Configuration.RecvTimeout;
                uint.TryParse (newRecvTimeout, out value);
                config.Configuration.RecvTimeout = value;
                config.Save ();
            }
        }

        void DrawDebugLogging ()
        {
            bool debugLogging = GUILayout.Toggle (config.Configuration.DebugLogging, debugLoggingText, toggleStyle, new GUILayoutOption[] { });
            if (debugLogging != config.Configuration.DebugLogging) {
                config.Configuration.DebugLogging = debugLogging;
                config.Save ();
            }
        }

        void DrawShowInfoWindow ()
        {
            bool value = GUILayout.Toggle (InfoWindow.Visible, showInfoWindowText, toggleStyle, new GUILayoutOption[] { });
            if (value != InfoWindow.Visible)
                InfoWindow.Visible = value;
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
    }
}
