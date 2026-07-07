using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        bool showAdvancedServerOptions;
        string maxTimePerUpdate;
        string recvTimeout;
        // Style settings
        internal readonly Color errorColor = Color.yellow;
        internal GUIStyle labelStyle, stretchyLabelStyle, fixedLabelStyle, textFieldStyle, longTextFieldStyle, stretchyTextFieldStyle,
            buttonStyle, toggleStyle, expandStyle, separatorStyle, lightStyle, errorLabelStyle, comboOptionsStyle, comboOptionStyle;
        const float windowWidth = 288f;
        const float textFieldWidth = 45f;
        const float longTextFieldWidth = 80f;
        const float fixedLabelWidth = 160f;
        const float serverInfoLabelWidth = 110f;
        const float indentWidth = 15f;
        float scaledIndentWidth;
        const int maxTimePerUpdateMaxLength = 20;
        const int recvTimeoutMaxLength = 20;
        // Text strings
        const string advancedModeText = "Show advanced settings";
        const string startAllServersText = "Start server";
        const string stopAllServersText = "Stop server";
        const string addServerText = "Add server";
        const string removeServerText = "Remove";
        const string startServerText = "Start";
        const string stopServerText = "Stop";
        const string editServerText = "Edit";
        const string saveServerText = "Save";
        const string serverOnlineText = "Server online";
        const string serverOfflineText = "Server offline";
        const string protocolText = "Protocol";
        internal const string protobufOverTcpText = "Protobuf over TCP";
        internal const string protobufOverWebSocketsText = "Protobuf over WebSockets";
        internal const string protobufOverSerialIOText = "Protobuf over SerialIO";
        const string unknownClientNameText = "<unknown>";
        const string noClientsConnectedText = "No clients connected";
        const string serverNotRunningText = "Server not running";
        const string advancedServerOptionsText = "Show advanced settings";
        const string autoStartServerText = "Auto-start server";
        const string autoAcceptConnectionsText = "Auto-accept new clients";
        const string confirmRemoveClientText = "Confirm disconnecting a client";
        const string pauseServerWithGameText = "Pause the server when the game pauses";
        const string oneRPCPerUpdateText = "One RPC per update";
        const string maxTimePerUpdateText = "Max. time per update";
        const string adaptiveRateControlText = "Adaptive rate control";
        const string blockingRecvText = "Blocking receives";
        const string recvTimeoutText = "Receive timeout";
        const string microsecondsUnitText = "us";
        const string debugLoggingText = "Debug logging";
        const string showInfoWindowText = "Show info";

        protected override void Init ()
        {
            core = Core.Instance;
            config = ConfigurationFile.Instance;

            var version = FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);
            Title = "kRPC v" + version.FileMajorPart + "." + version.FileMinorPart + "." + version.FileBuildPart;

            core.OnClientActivity += SawClientActivity;

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

            if (core.Servers.Count == 1)
                expandServers.Add (core.Servers [0].Id);
        }

        void OnDestroy()
        {
            core.OnClientActivity -= SawClientActivity;
        }

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
                comboOptionStyle.fontSize = scaledFontSize;
                GUILayoutExtensions.SetLightStyleSize (lightStyle, Style.lineHeight);
                Resized = true;
            }

            // Force window to resize to height of content
            if (Resized)
            {
                Position = new Rect(Position.x, Position.y, Position.width, 0f);
                Resized = false;
            }

            GUILayout.BeginVertical();

            DrawStartServer();
            GUILayoutExtensions.Separator(separatorStyle);

            var servers = core.Servers.ToList();
            foreach (var server in servers) {
                DrawServer(server);
                GUILayoutExtensions.Separator(separatorStyle);
            }

            DrawAddServer();
            GUILayoutExtensions.Separator(separatorStyle);

            if (Errors.Any()) {
                foreach (var error in Errors)
                    GUILayout.Label(error, errorLabelStyle);
                GUILayoutExtensions.Separator(separatorStyle);
            }

            DrawAdvancedServerOptions();
            DrawShowInfoWindow();
            GUILayout.EndVertical();

            GUI.DragWindow ();
        }

        void DrawStartServer()
        {
            var running = core.AnyRunning;
            var label = (running ? stopAllServersText : startAllServersText) + (core.Servers.Count > 1 ? "s" : string.Empty);
            GUI.enabled = !editServers.Any();
            if (GUILayout.Button(label, buttonStyle)) {
                Errors.Clear ();
                Resized = true;
                foreach (var server in core.Servers)
                    if (server.Running == running)
                        EventHandlerExtensions.Invoke(running ? OnStopServerPressed : OnStartServerPressed, this, new ServerEventArgs(server));
            }
            GUI.enabled = true;
        }

        void DrawServer (Server.Server server)
        {
            var running = server.Running;
            var editingServer = editServers.ContainsKey (server.Id);
            var expanded = expandServers.Contains (server.Id);

            GUILayout.BeginHorizontal ();
            var icons = Icons.Instance;
            // Vertically centre the expand/collapse arrow in the row; it is shorter
            // than the activity light and name and would otherwise sit at the top.
            // A fixed top space (rather than ExpandHeight + FlexibleSpace) is used so
            // the column cannot grab the window's spare height and push the arrow onto
            // its own line.
            GUILayout.BeginVertical (GUILayout.Width (20));
            GUILayout.Space (Mathf.Max (0f, (Style.lineHeight - 16f) / 2f));
            GUILayout.Label (new GUIContent (expanded ? icons.ButtonCollapse : icons.ButtonExpand, expanded ? "Collapse" : "Expand"),
                expandStyle, GUILayout.MaxWidth (20), GUILayout.MaxHeight (20));
            GUILayout.EndVertical ();
            GUILayoutExtensions.Light (running, lightStyle);
            if (!editingServer)
                GUILayout.Label (server.Name, labelStyle);
            else
                editServers [server.Id].DrawName ();
            GUILayout.EndHorizontal ();

            // Expand/collapse when the header row (icon, activity light and name) is
            // clicked. Skipped while editing, where the row holds the name text field.
            if (!editingServer && Event.current.type == EventType.MouseDown &&
                GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition)) {
                if (expanded)
                    expandServers.Remove (server.Id);
                else
                    expandServers.Add (server.Id);
                expanded = !expanded;
                Resized = true;
                Event.current.Use ();
            }

            if (editingServer) {
                editServers [server.Id].Draw ();
            } else if (expanded) {
                string protocol;
                if (server.Protocol == Protocol.ProtocolBuffersOverTCP)
                    protocol = protobufOverTcpText;
                else if (server.Protocol == Protocol.ProtocolBuffersOverWebsockets)
                    protocol = protobufOverWebSocketsText;
                else
                    protocol = protobufOverSerialIOText;
                DrawServerInfoRow (protocolText, protocol);
                DrawServerInfoLines (server.Address);
                DrawServerInfoLines (server.Info);
                DrawClients (server);
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = !editingServer;
            if (GUILayout.Button(running ? stopServerText : startServerText, buttonStyle)) {
                Errors.Clear();
                Resized = true;
                EventHandlerExtensions.Invoke(running ? OnStopServerPressed : OnStartServerPressed, this, new ServerEventArgs(server));
            }
            GUI.enabled = !running;
            if (GUILayout.Button(editingServer ? saveServerText : editServerText, buttonStyle))
            {
                if (editingServer)
                {
                    var newServer = editServers[server.Id].Save();
                    if (newServer != null)
                    {
                        editServers.Remove(server.Id);
                        config.Configuration.ReplaceServer(newServer);
                        config.Save();
                        core.Replace(newServer.Create());
                    }
                }
                else {
                    editServers[server.Id] = new EditServer(this, config.Configuration.GetServer(server.Id));
                }
                Resized = true;
            }
            GUI.enabled = !editingServer && !running;
            if (GUILayout.Button(removeServerText, buttonStyle))
            {
                config.Configuration.RemoveServer(server.Id);
                config.Save();
                core.Remove(server.Id);
                Resized = true;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal ();
        }

        // Draw a name/value row with an aligned name column, for the server details
        // "table". Matches the style of the info window.
        void DrawServerInfoRow (string name, string value)
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Label (name, labelStyle, GUILayout.Width (serverInfoLabelWidth * GameSettings.UI_SCALE));
            GUILayout.Label (value, stretchyLabelStyle);
            GUILayout.EndHorizontal ();
        }

        // Split a multi-line server detail string (e.g. Address or Info) into name/value
        // rows, splitting each line on its "name = value" or "name: value" separator.
        // Lines with no separator are shown in the value column.
        void DrawServerInfoLines (string text)
        {
            foreach (var line in text.Split ('\n')) {
                var trimmed = line.Trim ();
                if (trimmed.Length == 0)
                    continue;
                var separator = trimmed.IndexOf (" = ", StringComparison.Ordinal);
                var separatorLength = 3;
                if (separator < 0) {
                    separator = trimmed.IndexOf (": ", StringComparison.Ordinal);
                    separatorLength = 2;
                }
                if (separator >= 0)
                    DrawServerInfoRow (trimmed.Substring (0, separator), trimmed.Substring (separator + separatorLength));
                else
                    DrawServerInfoRow (string.Empty, trimmed);
            }
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
                GUILayout.Label (server.Running ? noClientsConnectedText : serverNotRunningText, labelStyle);
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

        void DrawAdvancedServerOptions ()
        {
            GUILayout.BeginHorizontal ();
            DrawAdvancedServerOptionsToggle ();
            GUILayout.EndHorizontal ();

            if (showAdvancedServerOptions) {
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
                DrawPauseServerWithGameToggle ();
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

        void DrawAdvancedServerOptionsToggle ()
        {
            bool value = GUILayout.Toggle (showAdvancedServerOptions, advancedServerOptionsText, toggleStyle, new GUILayoutOption[] { });
            if (value != showAdvancedServerOptions) {
                showAdvancedServerOptions = value;
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
            bool autoAcceptConnections = GUILayout.Toggle (config.Configuration.AutoAcceptConnections, autoAcceptConnectionsText, toggleStyle, new GUILayoutOption [] { });
            if (autoAcceptConnections != config.Configuration.AutoAcceptConnections) {
                config.Configuration.AutoAcceptConnections = autoAcceptConnections;
                config.Save ();
            }
        }

        void DrawPauseServerWithGameToggle ()
        {
            bool pauseServerWithGame = GUILayout.Toggle (config.Configuration.PauseServerWithGame, pauseServerWithGameText, toggleStyle, new GUILayoutOption [] { });
            if (pauseServerWithGame != config.Configuration.PauseServerWithGame) {
                config.Configuration.PauseServerWithGame = pauseServerWithGame;
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
            uint value;
            bool valid = uint.TryParse (maxTimePerUpdate, out value);
            var newMaxTimePerUpdate = GUILayoutExtensions.FilterDigits (
                GUILayoutExtensions.ValidatedTextField (maxTimePerUpdate, maxTimePerUpdateMaxLength, longTextFieldStyle, valid, errorColor));
            GUILayout.Label (microsecondsUnitText, labelStyle);
            if (newMaxTimePerUpdate != maxTimePerUpdate) {
                maxTimePerUpdate = newMaxTimePerUpdate;
                if (uint.TryParse (maxTimePerUpdate, out value)) {
                    config.Configuration.MaxTimePerUpdate = value;
                    config.Save ();
                }
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
            uint value;
            bool valid = uint.TryParse (recvTimeout, out value);
            var newRecvTimeout = GUILayoutExtensions.FilterDigits (
                GUILayoutExtensions.ValidatedTextField (recvTimeout, recvTimeoutMaxLength, longTextFieldStyle, valid, errorColor));
            GUILayout.Label (microsecondsUnitText, labelStyle);
            if (newRecvTimeout != recvTimeout) {
                recvTimeout = newRecvTimeout;
                if (uint.TryParse (recvTimeout, out value)) {
                    config.Configuration.RecvTimeout = value;
                    config.Save ();
                }
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

        void SawClientActivity (object sender, ClientActivityEventArgs e)
        {
            lastClientActivity [e.Client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
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
