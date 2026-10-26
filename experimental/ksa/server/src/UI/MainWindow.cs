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
        internal const string GrpcText = "gRPC";
        const string UnknownClientNameText = "<unknown>";
        const string MicrosecondsUnitText = "us";
        // Milliseconds a client stays marked active after it sends or receives
        const long ActivityInterval = 100;

        readonly Core core;
        readonly ConfigurationFile config;
        readonly Addon addon;
        readonly HashSet<Guid> expandServers = new HashSet<Guid> ();
        readonly Dictionary<Guid, EditServer> editServers = new Dictionary<Guid, EditServer> ();
        readonly Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();
        string maxTimePerUpdate;
        string recvTimeout;
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
            maxTimePerUpdate = config.Configuration.MaxTimePerUpdate.ToString (CultureInfo.InvariantCulture);
            recvTimeout = config.Configuration.RecvTimeout.ToString (CultureInfo.InvariantCulture);
            tickHoldTimeout = config.Configuration.TickHoldTimeout.ToString (CultureInfo.InvariantCulture);
            if (core.Servers.Count == 1)
                expandServers.Add (core.Servers [0].Id);
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
                var server = new Configuration.Server ();
                config.Configuration.Servers.Add (server);
                config.Save ();
                core.Add (addon.CreateServer (server));
                expandServers.Add (server.Id);
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
            var clients = core.RPCClients.Count ();
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

            ImGui.SetNextItemOpen (expandServers.Contains (server.Id));
            var expanded = ImGui.CollapsingHeader (server.Name + "###header");
            if (expanded)
                expandServers.Add (server.Id);
            else
                expandServers.Remove (server.Id);
            ImGui.SameLine (ImGui.GetContentRegionAvail ().X - ImGui.CalcTextSize ("OFFLINE").X);
            if (running)
                ImGui.TextColored (ConsoleStyle.Positive, "ONLINE");
            else
                ImGui.TextColored (ConsoleStyle.TextMuted, "OFFLINE");

            if (edit != null) {
                edit.Draw ();
            } else if (expanded) {
                ConsoleWidgets.Readout ("Protocol", ProtocolName (server.Protocol));
                DrawServerInfoLines (server.Address);
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
                    expandServers.Add (server.Id);
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
                expandServers.Remove (server.Id);
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
            case ConfigurationFile.GrpcProtocol:
                return GrpcText;
            default:
                return ProtobufOverSerialIOText;
            }
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
            changed |= DrawCheckboxRow ("One RPC per update", "one_rpc", c.OneRPCPerUpdate, v => c.OneRPCPerUpdate = v);
            changed |= DrawMicrosecondsRow ("Max. time per update", "max_time", ref maxTimePerUpdate, v => c.MaxTimePerUpdate = v);
            changed |= DrawCheckboxRow ("Adaptive rate control", "adaptive", c.AdaptiveRateControl, v => c.AdaptiveRateControl = v);
            changed |= DrawCheckboxRow ("Blocking receives", "blocking_recv", c.BlockingRecv, v => c.BlockingRecv = v);
            changed |= DrawMicrosecondsRow ("Receive timeout", "recv_timeout", ref recvTimeout, v => c.RecvTimeout = v);
            changed |= DrawMicrosecondsRow ("Tick hold timeout", "tick_hold", ref tickHoldTimeout, v => c.TickHoldTimeout = v);
            changed |= DrawCheckboxRow ("Debug logging", "debug_logging", c.DebugLogging, v => c.DebugLogging = v);
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

        // A time in microseconds, applied whenever the text holds a valid value
        static bool DrawMicrosecondsRow (string label, string id, ref string text, Action<uint> set)
        {
            var valid = uint.TryParse (text, NumberStyles.None, CultureInfo.InvariantCulture, out var value);
            var changed = DrawTextRow (label + " (" + MicrosecondsUnitText + ")", id, ref text, !valid, true);
            valid = uint.TryParse (text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
            if (!valid) {
                ImGui.TextColored (ConsoleStyle.Danger, label + " must be a whole number of microseconds");
                return false;
            }
            if (changed)
                set (value);
            return changed;
        }
    }
}
