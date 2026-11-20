using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        public ProblemsWindow ProblemsWindow { private get; set; }

        public ClientDisconnectDialog ClientDisconnectDialog { get; set; }

        public List<string> Errors { get; private set; }

        public event EventHandler<ServerEventArgs> OnStartServerPressed;
        public event EventHandler<ServerEventArgs> OnStopServerPressed;

        readonly IDictionary<Guid, EditServer> editServers = new Dictionary<Guid, EditServer> ();
        readonly Dictionary<string, string> allowedOriginsTexts = new Dictionary<string, string> ();
        readonly IDictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        const long lastActivityMillisecondsInterval = 100;

        internal bool Resized { get; set; }

        // Rows whose number changes outside the window, i.e. clients, server info and errors
        int rows;
        int previousRows;

        bool showAdvancedServerOptions;
        readonly object presetComboId = new object ();
        // The presets' names, then the entry for settings that match none
        static readonly string[] presetNames =
            Configuration.Presets.Select (x => x.Name).Concat (new [] { "Custom" }).ToArray ();
        string targetFrameRate;
        string maxTimePerFrame;
        string maxCallWait;
        string tickHoldTimeout;
        // Style settings
        internal readonly Color errorColor = Color.yellow;
        internal GUIStyle labelStyle, stretchyLabelStyle, fixedLabelStyle, longTextFieldStyle, stretchyTextFieldStyle,
            buttonStyle, toggleStyle, expandStyle, separatorStyle, lightStyle, errorLabelStyle, comboOptionsStyle, comboOptionStyle;
        const float windowWidth = 310f;
        const float longTextFieldWidth = 80f;
        const float fixedLabelWidth = 160f;
        const float serverInfoLabelWidth = 110f;
        const float indentWidth = 15f;
        float scaledIndentWidth;
        const int numberMaxLength = 20;
        // Text strings
        const string startAllServersText = "Start server";
        const string stopAllServersText = "Stop server";
        const string addServerText = "Add server";
        const string removeServerText = "Remove";
        const string startServerText = "Start";
        const string stopServerText = "Stop";
        const string editServerText = "Edit";
        const string saveServerText = "Save";
        const string protocolText = "Protocol";
        const string addressText = "Address";
        const string originsText = "Origins";
        const string sameOriginText = "Same origin only";
        // The name of each protocol, indexed by its Protocol value
        internal static readonly string[] protocolNames = {
            "Protobuf over TCP/IP",
            "Protobuf over WebSockets",
            "Protobuf over SerialIO",
            "Protobuf over local socket",
            "JSON over HTTP"
        };
        const string unknownClientNameText = "<unknown>";
        const string noClientsConnectedText = "No clients connected";
        const string serverNotRunningText = "Server not running";
        const string advancedServerOptionsText = "Show advanced settings";
        const string autoStartServerText = "Auto-start server";
        const string autoAcceptConnectionsText = "Auto-accept new clients";
        const string confirmRemoveClientText = "Confirm disconnecting a client";
        const string pauseServerWithGameText = "Pause the server when the game pauses";
        const string presetText = "Preset";
        const string adaptiveRateControlText = "Adaptive rate control";
        const string targetFrameRateText = "Target frame rate";
        const string maxTimePerFrameText = "Max. time per frame";
        const string maxCallWaitText = "Max. call wait";
        const string tickHoldTimeoutText = "Tick hold timeout";
        const string microsecondsUnitText = "us";
        const string framesPerSecondUnitText = "fps";
        const string debugLoggingText = "Debug logging";
        const string showInfoWindowText = "Show info";
        const string showProblemsWindowText = "Show problems";

        protected override void Init ()
        {
            core = Core.Instance;
            config = ConfigurationFile.Instance;

            var version = FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);
            Title = "kRPC v" + version.FileMajorPart + "." + version.FileMinorPart + "." + version.FileBuildPart;

            core.OnClientActivity += SawClientActivity;
            core.OnClientDisconnected += ForgetClient;

            Style.fixedWidth = windowWidth;

            var skin = Skin.DefaultSkin;

            labelStyle = new GUIStyle (skin.label);
            labelStyle.margin = new RectOffset (0, 0, 0, 0);

            stretchyLabelStyle = new GUIStyle (skin.label);
            stretchyLabelStyle.margin = new RectOffset (0, 0, 0, 0);
            stretchyLabelStyle.stretchWidth = true;

            // No vertical margin, as the text fields beside these labels have none
            fixedLabelStyle = new GUIStyle (skin.label);
            fixedLabelStyle.margin = new RectOffset (skin.label.margin.left, skin.label.margin.right, 0, 0);

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
            ShowRateSettings ();
            tickHoldTimeout = config.Configuration.TickHoldTimeout.ToString (CultureInfo.CurrentCulture);
        }

        void OnDestroy()
        {
            core.OnClientActivity -= SawClientActivity;
            core.OnClientDisconnected -= ForgetClient;
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

            rows = 0;
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
                foreach (var error in Errors) {
                    GUILayout.Label(error, errorLabelStyle);
                    rows++;
                }
                GUILayoutExtensions.Separator(separatorStyle);
            }

            DrawAdvancedServerOptions();
            DrawShowInfoWindow();
            DrawShowProblemsWindow();
            GUILayout.EndVertical();

            // The window grows to fit its content, but only shrinks when its height is reset
            if (rows < previousRows)
                Resized = true;
            previousRows = rows;

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
            var expandedServers = config.Configuration.ExpandedServers;
            var expanded = expandedServers.Contains (server.Id);

            GUILayout.BeginHorizontal ();
            var icons = Icons.Instance;
            // Vertically center the expand/collapse arrow in the row. It is shorter than
            // the activity light and name, and would otherwise sit at the top. A fixed top
            // space keeps the column from grabbing the window's spare height and pushing
            // the arrow onto its own line
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
            // An HTTP server's requests share one internal client, so it lists no clients
            var listsClients = server.Protocol != Protocol.JSONOverHTTP;
            var clients = listsClients ? server.Clients.Count () : 0;
            if (clients > 0) {
                GUILayout.FlexibleSpace ();
                GUILayout.Label (clients.ToString (CultureInfo.CurrentCulture) + (clients == 1 ? " client" : " clients"), labelStyle);
            }
            GUILayout.EndHorizontal ();

            // Expand/collapse when the header row is clicked. Skipped while editing, where
            // the row holds the name text field.
            if (!editingServer && Event.current.type == EventType.MouseDown &&
                GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition)) {
                if (expanded)
                    expandedServers.Remove (server.Id);
                else
                    expandedServers.Add (server.Id);
                config.Save ();
                expanded = !expanded;
                Resized = true;
                Event.current.Use ();
            }

            if (editingServer) {
                editServers [server.Id].Draw ();
            } else if (expanded) {
                DrawServerInfoRow (protocolText, protocolNames [(int)server.Protocol]);
                DrawServerInfoRow (addressText, server.Address);
                if (server.Protocol == Protocol.JSONOverHTTP)
                    DrawServerInfoRow (originsText, AllowedOriginsText (server));
                DrawServerInfoLines (server.Info);
                if (listsClients)
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

        // The origins a server allows, from its saved settings. The text is cached by
        // setting, as the window is drawn every frame.
        string AllowedOriginsText (KRPC.Server.Server server)
        {
            var saved = config.Configuration.Servers.FirstOrDefault (x => x.Id == server.Id);
            var setting = Configuration.Server.DefaultAllowedOrigins (server.Protocol);
            if (saved != null) {
                string value;
                if (saved.Settings.TryGetValue (Configuration.Server.SettingsPrefix (server.Protocol) + "allowed_origins", out value))
                    setting = value;
            }
            string text;
            if (!allowedOriginsTexts.TryGetValue (setting, out text)) {
                text = sameOriginText;
                if (KRPC.Server.AllowedOrigins.IsValid (setting)) {
                    var origins = new KRPC.Server.AllowedOrigins (setting);
                    if (!origins.None)
                        text = origins.ToString ();
                }
                allowedOriginsTexts [setting] = text;
            }
            return text;
        }

        // Draw a name/value row with an aligned name column, for the server details
        // "table". Matches the style of the info window.
        void DrawServerInfoRow (string name, string value)
        {
            rows++;
            GUILayout.BeginHorizontal ();
            GUILayout.Label (name, labelStyle, GUILayout.Width (serverInfoLabelWidth * GameSettings.UI_SCALE));
            GUILayout.Label (value, stretchyLabelStyle);
            GUILayout.EndHorizontal ();
        }

        // Split a multi-line server detail string (e.g. Info) into name/value
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

        void DrawClients (KRPC.Server.Server server)
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
                    rows++;
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
                var server = config.Configuration.NewServer (Protocol.ProtocolBuffersOverTCP);
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

                var settings = config.Configuration;
                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawDebugLogging ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                tickHoldTimeout = DrawNumberField (tickHoldTimeoutText, tickHoldTimeout, microsecondsUnitText,
                                                   0, uint.MaxValue, x => settings.TickHoldTimeout = x);
                GUILayout.EndHorizontal ();

                // The rows below the preset are the settings a preset sets
                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawPreset ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                DrawAdaptiveRateControlToggle ();
                GUILayout.EndHorizontal ();

                // The target applies only with adaptive rate control
                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                GUI.enabled = settings.AdaptiveRateControl;
                targetFrameRate = DrawNumberField (targetFrameRateText, targetFrameRate, framesPerSecondUnitText,
                                                   Configuration.TargetFrameRateMin, Configuration.TargetFrameRateMax,
                                                   x => settings.TargetFrameRate = x);
                GUI.enabled = true;
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                maxTimePerFrame = DrawNumberField (maxTimePerFrameText, maxTimePerFrame, microsecondsUnitText,
                                                   Configuration.MaxTimePerFrameMin, Configuration.MaxTimePerFrameMax,
                                                   x => settings.MaxTimePerFrame = x);
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();
                GUILayout.Space (scaledIndentWidth);
                maxCallWait = DrawNumberField (maxCallWaitText, maxCallWait, microsecondsUnitText,
                                               0, uint.MaxValue, x => settings.MaxCallWait = x);
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

        void DrawPreset ()
        {
            var settings = config.Configuration;
            GUILayout.Label (presetText, fixedLabelStyle);
            var current = settings.CurrentPreset;
            var selected = current == null ? presetNames.Length - 1 : Configuration.Presets.IndexOf (current);
            var chosen = GUILayoutExtensions.ComboBox (presetComboId, selected, presetNames, buttonStyle, comboOptionsStyle, comboOptionStyle);
            // Choosing the custom entry leaves the settings as they are
            if (chosen != selected && chosen < Configuration.Presets.Count) {
                Configuration.Presets [chosen].Apply (settings);
                config.Save ();
                ShowRateSettings ();
            }
        }

        /// <summary>
        /// Set the text of the rate settings' fields from the configuration.
        /// </summary>
        void ShowRateSettings ()
        {
            var settings = config.Configuration;
            targetFrameRate = settings.TargetFrameRate.ToString (CultureInfo.CurrentCulture);
            maxTimePerFrame = settings.MaxTimePerFrame.ToString (CultureInfo.CurrentCulture);
            maxCallWait = settings.MaxCallWait.ToString (CultureInfo.CurrentCulture);
        }

        void DrawAdaptiveRateControlToggle ()
        {
            bool adaptiveRateControl = GUILayout.Toggle (config.Configuration.AdaptiveRateControl, adaptiveRateControlText, toggleStyle, new GUILayoutOption[] { });
            if (adaptiveRateControl != config.Configuration.AdaptiveRateControl) {
                config.Configuration.AdaptiveRateControl = adaptiveRateControl;
                config.Save ();
            }
        }

        /// <summary>
        /// Draw a field for a whole number setting, with its label and unit, and apply a valid
        /// edit. Returns the field's text.
        /// </summary>
        string DrawNumberField (string label, string text, string unit, uint min, uint max, Action<uint> apply)
        {
            GUILayout.Label (label, fixedLabelStyle);
            uint value;
            bool valid = uint.TryParse (text, out value) && value >= min && value <= max;
            var newText = GUILayoutExtensions.FilterDigits (
                GUILayoutExtensions.ValidatedTextField (text, numberMaxLength, longTextFieldStyle, valid, errorColor));
            GUILayout.Label (unit, labelStyle);
            if (newText != text && uint.TryParse (newText, out value) && value >= min && value <= max) {
                apply (value);
                config.Save ();
            }
            return newText;
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

        void DrawShowProblemsWindow ()
        {
            if (KRPC.Service.ServiceProblems.Digest == null)
                return;
            bool value = GUILayout.Toggle (ProblemsWindow.Visible, showProblemsWindowText, toggleStyle, new GUILayoutOption[] { });
            if (value != ProblemsWindow.Visible)
                ProblemsWindow.Visible = value;
        }

        void SawClientActivity (object sender, ClientActivityEventArgs e)
        {
            lastClientActivity [e.Client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        void ForgetClient (object sender, ClientDisconnectedEventArgs e)
        {
            lastClientActivity.Remove (e.Client);
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
