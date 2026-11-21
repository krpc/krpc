using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.Server;
using KSA;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// The kRPC window: starts and stops the servers, edits them, lists their clients, and
    /// holds the server settings.
    /// </summary>
    sealed class MainWindow : ConsoleWindow
    {
        internal const string ProtobufOverTcpText = "Protobuf over TCP";
        internal const string ProtobufOverWebSocketsText = "Protobuf over WebSockets";
        internal const string ProtobufOverSerialIOText = "Protobuf over SerialIO";
        internal const string ProtobufOverLocalSocketText = "Protobuf over local socket";
        internal const string JsonOverHttpText = "JSON over HTTP";
        internal const string GrpcText = "gRPC";
        const string UnknownClientNameText = "<unknown>";
        const string MicrosecondsUnitText = "us";
        const string FramesPerSecondUnitText = "fps";
        // The presets' names, then the entry for settings that match none
        static readonly string[] PresetNames =
            Configuration.Presets.Select (x => x.Name).Concat (new [] { "Custom" }).ToArray ();
        // Milliseconds a client stays marked active after it sends or receives
        const long ActivityInterval = 100;

        readonly Core core;
        readonly ConfigurationFile config;
        readonly Addon addon;
        readonly Dictionary<Guid, EditServer> editServers = new Dictionary<Guid, EditServer> ();
        readonly Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        string targetFrameRate;
        string maxTimePerFrame;
        string maxCallWait;
        string tickHoldTimeout;

        /// <summary>
        /// Errors from the last start of a server or save of its settings.
        /// </summary>
        public List<string> Errors { get; } = new List<string> ();

        public MainWindow (Addon addon, Core core, ConfigurationFile config)
            : base ("kRPC", "KRPC-SRV", new float2 (400, 520), config.Configuration.MainWindowVisible,
                config.Configuration.MainWindowPosition)
        {
            this.addon = addon;
            this.core = core;
            this.config = config;
            ShowRateSettings ();
            tickHoldTimeout = config.Configuration.TickHoldTimeout.ToString (CultureInfo.InvariantCulture);
            core.OnClientActivity += (s, e) => lastClientActivity [e.Client] = Environment.TickCount64;
            core.OnClientDisconnected += (s, e) => lastClientActivity.Remove (e.Client);
        }

        protected override void DrawWindow (IViewport viewport)
        {
            DrawStartServer ();
            ConsoleWidgets.Rule ();
            foreach (var server in core.Servers.ToList ()) {
                DrawServer (server);
                ConsoleWidgets.Rule ();
            }
            if (ConsoleWidgets.Button ("Add server")) {
                var server = config.Configuration.NewServer (Protocol.ProtocolBuffersOverTCP);
                config.Configuration.Servers.Add (server);
                config.Configuration.ExpandedServers.Add (server.Id);
                config.Save ();
                core.Add (addon.CreateServer (server));
            }
            if (Errors.Count > 0) {
                ConsoleWidgets.Rule ();
                foreach (var error in Errors)
                    ImGui.TextColored (ConsoleStyle.Danger, error);
            }
            ConsoleWidgets.Rule ();
            DrawSettings ();
        }

        protected override void DrawConsoleFooter ()
        {
            var clients = core.Clients.Count ();
            var status = (core.AnyRunning ? "ONLINE" : "OFFLINE") + "  " +
                clients.ToString (CultureInfo.InvariantCulture) + (clients == 1 ? " CLIENT" : " CLIENTS");
            ConsoleStyle.FooterStatus (status, false);
        }

        void DrawStartServer ()
        {
            var running = core.AnyRunning;
            var label = (running ? "Stop server" : "Start server") + (core.Servers.Count > 1 ? "s" : string.Empty);
            ImGui.BeginDisabled (editServers.Count > 0);
            if (running ? ConsoleWidgets.DangerButton (label) : ConsoleWidgets.PositiveButton (label)) {
                Errors.Clear ();
                foreach (var server in core.Servers) {
                    if (server.Running == running) {
                        if (running)
                            addon.StopServer (server);
                        else
                            addon.StartServer (server);
                    }
                }
            }
            ImGui.EndDisabled ();
        }

        void DrawServer (Server.Server server)
        {
            var running = server.Running;
            editServers.TryGetValue (server.Id, out var edit);
            ImGui.PushID (server.Id.ToString ());

            var expandedServers = config.Configuration.ExpandedServers;
            var wasExpanded = expandedServers.Contains (server.Id);
            ImGui.SetNextItemOpen (wasExpanded);
            var expanded = ImGui.CollapsingHeader (server.Name + "###header");
            if (expanded != wasExpanded) {
                if (expanded)
                    expandedServers.Add (server.Id);
                else
                    expandedServers.Remove (server.Id);
                config.Save ();
            }
            var statusWidth = ImGui.CalcTextSize ("OFFLINE").X;
            var clients = server.Clients.Count ();
            if (clients > 0) {
                var text = clients.ToString (CultureInfo.InvariantCulture) + (clients == 1 ? " client" : " clients");
                ImGui.SameLine (ImGui.GetContentRegionAvail ().X - statusWidth - ImGui.CalcTextSize (text + "  ").X);
                ImGui.TextColored (ConsoleStyle.TextMuted, text);
            }
            ImGui.SameLine (ImGui.GetContentRegionAvail ().X - statusWidth);
            if (running)
                ImGui.TextColored (ConsoleStyle.Positive, "ONLINE");
            else
                ImGui.TextColored (ConsoleStyle.TextMuted, "OFFLINE");

            if (edit != null) {
                edit.Draw ();
            } else if (expanded) {
                ConsoleWidgets.Readout ("Protocol", ProtocolName (server.Protocol));
                DrawServerInfoLines (server.Address);
                if (server.Protocol == Protocol.ProtocolBuffersOverWebsockets || server.Protocol == Protocol.JSONOverHTTP)
                    ConsoleWidgets.Readout ("Origins", AllowedOriginsText (server));
                DrawServerInfoLines (server.Info);
                DrawClients (server);
            }

            ImGui.BeginDisabled (edit != null);
            if (ConsoleWidgets.Button (running ? "Stop" : "Start")) {
                Errors.Clear ();
                if (running)
                    addon.StopServer (server);
                else
                    addon.StartServer (server);
            }
            ImGui.EndDisabled ();
            ImGui.SameLine ();
            ImGui.BeginDisabled (running);
            if (ConsoleWidgets.Button (edit != null ? "Save" : "Edit")) {
                if (edit != null) {
                    var newServer = edit.Save (Errors);
                    if (newServer != null) {
                        try {
                            core.Replace (addon.CreateServer (newServer));
                            editServers.Remove (server.Id);
                            config.Configuration.ReplaceServer (newServer);
                            config.Save ();
                        } catch (Exception e) {
                            Errors.Add (e.Message);
                        }
                    }
                } else {
                    Errors.Clear ();
                    editServers [server.Id] = new EditServer (config.Configuration.GetServer (server.Id));
                    if (expandedServers.Add (server.Id))
                        config.Save ();
                }
            }
            ImGui.EndDisabled ();
            if (edit != null) {
                ImGui.SameLine ();
                if (ConsoleWidgets.Button ("Cancel")) {
                    Errors.Clear ();
                    editServers.Remove (server.Id);
                }
            }
            ImGui.SameLine ();
            ImGui.BeginDisabled (edit != null || running);
            if (ConsoleWidgets.DangerButton ("Remove")) {
                config.Configuration.RemoveServer (server.Id);
                config.Save ();
                core.Remove (server.Id);
            }
            ImGui.EndDisabled ();
            ImGui.PopID ();
        }

        internal static string ProtocolName (Protocol protocol)
        {
            switch (protocol) {
            case Protocol.ProtocolBuffersOverTCP:
                return ProtobufOverTcpText;
            case Protocol.ProtocolBuffersOverWebsockets:
                return ProtobufOverWebSocketsText;
            case Protocol.ProtocolBuffersOverLocalSocket:
                return ProtobufOverLocalSocketText;
            case Protocol.JSONOverHTTP:
                return JsonOverHttpText;
            case ConfigurationFile.GrpcProtocol:
                return GrpcText;
            default:
                return ProtobufOverSerialIOText;
            }
        }

        // The origins a WebSockets or HTTP server allows, from its saved settings
        string AllowedOriginsText (Server.Server server)
        {
            var key = Configuration.Server.SettingsPrefix (server.Protocol) + "allowed_origins";
            if (!config.Configuration.GetServer (server.Id).Settings.TryGetValue (key, out var setting))
                setting = Configuration.Server.DefaultAllowedOrigins (server.Protocol);
            var origins = new AllowedOrigins (AllowedOrigins.IsValid (setting) ? setting : null);
            if (!origins.None)
                return origins.ToString ();
            return server.Protocol == Protocol.JSONOverHTTP ? "Same origin only" : "No web pages";
        }

        // Split a multi-line server detail, such as Server.Address or Server.Info, into
        // readout rows on each line's "name = value" or "name: value" separator
        static void DrawServerInfoLines (string text)
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
                    ConsoleWidgets.Readout (trimmed.Substring (0, separator), trimmed.Substring (separator + separatorLength));
                else
                    ConsoleWidgets.ValueText (trimmed);
            }
        }

        void DrawClients (Server.Server server)
        {
            var descriptions = new List<KeyValuePair<IClient, string>> ();
            foreach (var client in server.Clients) {
                try {
                    var name = client.Name;
                    descriptions.Add (new KeyValuePair<IClient, string> (
                        client, (name.Length == 0 ? UnknownClientNameText : name) + " @ " + client.Address));
                } catch (ClientDisconnectedException) {
                }
            }
            if (descriptions.Count == 0) {
                ImGui.TextDisabled (server.Running ? "No clients connected" : "Server not running");
                return;
            }
            foreach (var entry in descriptions) {
                var client = entry.Key;
                ImGui.PushID (client.Guid.ToString ());
                ImGui.AlignTextToFramePadding ();
                if (IsClientActive (client))
                    ImGui.TextColored (ConsoleStyle.Positive, entry.Value);
                else
                    ImGui.TextColored (ConsoleStyle.TextPrimary, entry.Value);
                ImGui.SameLine (ImGui.GetContentRegionAvail ().X - ConsoleWidgets.ButtonWidth ("Disconnect") + ImGui.GetCursorPosX ());
                if (ConsoleWidgets.DangerButton ("Disconnect")) {
                    if (config.Configuration.ConfirmRemoveClient)
                        addon.ConfirmDisconnect (client);
                    else
                        client.Close ();
                }
                ImGui.PopID ();
            }
        }

        bool IsClientActive (IClient client)
        {
            return lastClientActivity.TryGetValue (client, out var lastActivity) &&
                Environment.TickCount64 - lastActivity < ActivityInterval;
        }

        void DrawSettings ()
        {
            if (!ImGui.CollapsingHeader ("Advanced settings"))
                return;
            var c = config.Configuration;
            var changed = false;
            changed |= DrawCheckboxRow ("Auto-start server", "auto_start", c.AutoStartServers, v => c.AutoStartServers = v);
            changed |= DrawCheckboxRow ("Auto-accept new clients", "auto_accept", c.AutoAcceptConnections, v => c.AutoAcceptConnections = v);
            changed |= DrawCheckboxRow ("Confirm disconnecting a client", "confirm_remove", c.ConfirmRemoveClient, v => c.ConfirmRemoveClient = v);
            changed |= DrawCheckboxRow ("Pause the server with the game", "pause", c.PauseServerWithGame, v => c.PauseServerWithGame = v);
            changed |= DrawCheckboxRow ("Debug logging", "debug_logging", c.DebugLogging, v => c.DebugLogging = v);
            changed |= DrawNumberRow ("Tick hold timeout", MicrosecondsUnitText, "tick_hold", ref tickHoldTimeout,
                0, uint.MaxValue, v => c.TickHoldTimeout = v);
            // The rows below the preset are the settings a preset sets
            changed |= DrawPreset ();
            changed |= DrawCheckboxRow ("Adaptive rate control", "adaptive", c.AdaptiveRateControl, v => c.AdaptiveRateControl = v);
            // The target applies only with adaptive rate control
            ImGui.BeginDisabled (!c.AdaptiveRateControl);
            changed |= DrawNumberRow ("Target frame rate", FramesPerSecondUnitText, "target_frame_rate", ref targetFrameRate,
                Configuration.TargetFrameRateMin, Configuration.TargetFrameRateMax, v => c.TargetFrameRate = v);
            ImGui.EndDisabled ();
            changed |= DrawNumberRow ("Max. time per frame", MicrosecondsUnitText, "max_time_per_frame", ref maxTimePerFrame,
                Configuration.MaxTimePerFrameMin, Configuration.MaxTimePerFrameMax, v => c.MaxTimePerFrame = v);
            changed |= DrawNumberRow ("Max. call wait", MicrosecondsUnitText, "max_call_wait", ref maxCallWait,
                0, uint.MaxValue, v => c.MaxCallWait = v);
            if (changed)
                config.Save ();
            DrawCheckboxRow ("Show info", "show_info", addon.InfoWindow.IsShown, addon.InfoWindow.SetShown);
        }

        static bool DrawCheckboxRow (string label, string id, bool value, Action<bool> set)
        {
            ConsoleWidgets.BeginRow (label);
            var changed = ConsoleWidgets.Checkbox (id, ref value, false);
            ConsoleWidgets.EndRow ();
            if (changed)
                set (value);
            return changed;
        }

        bool DrawPreset ()
        {
            var c = config.Configuration;
            var current = c.CurrentPreset;
            var selected = current == null ? PresetNames.Length - 1 : Configuration.Presets.IndexOf (current);
            // Choosing the custom entry leaves the settings as they are
            if (!DrawComboRow ("Preset", "preset", PresetNames, ref selected) || selected >= Configuration.Presets.Count)
                return false;
            Configuration.Presets [selected].Apply (c);
            ShowRateSettings ();
            return true;
        }

        // Set the text of the rate settings' fields from the configuration
        void ShowRateSettings ()
        {
            var c = config.Configuration;
            targetFrameRate = c.TargetFrameRate.ToString (CultureInfo.InvariantCulture);
            maxTimePerFrame = c.MaxTimePerFrame.ToString (CultureInfo.InvariantCulture);
            maxCallWait = c.MaxCallWait.ToString (CultureInfo.InvariantCulture);
        }

        // A whole number from min to max, applied whenever the text holds a valid value
        static bool DrawNumberRow (string label, string unit, string id, ref string text, uint min, uint max, Action<uint> set)
        {
            var changed = DrawTextRow (label + " (" + unit + ")", id, ref text, !Valid (text, min, max), true);
            if (!Valid (text, min, max)) {
                var range = max == uint.MaxValue ? string.Empty :
                    string.Format (CultureInfo.InvariantCulture, " from {0} to {1}", min, max);
                ImGui.TextColored (ConsoleStyle.Danger, label + " must be a whole number" + range);
                return false;
            }
            if (changed)
                set (uint.Parse (text, NumberStyles.None, CultureInfo.InvariantCulture));
            return changed;
        }

        static bool Valid (string text, uint min, uint max)
        {
            return uint.TryParse (text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) &&
                value >= min && value <= max;
        }
    }
}
