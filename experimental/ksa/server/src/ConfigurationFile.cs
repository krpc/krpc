using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KRPC.Server;
using Tomlet;
using Tomlet.Models;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// Server settings, stored in settings.toml in the mod folder. Loads them into
    /// <see cref="Configuration.Instance"/>, and saves them from it.
    /// </summary>
    sealed class ConfigurationFile
    {
        readonly string path;

        public ConfigurationFile (string path)
        {
            this.path = path;
            // Defaults that differ from the core's, for a game with no other way to start the server
            Configuration.AutoStartServers = true;
            Configuration.AutoAcceptConnections = true;
            Configuration.MainWindowVisible = false;
        }

        public Configuration Configuration {
            get { return Configuration.Instance; }
        }

        public void Load ()
        {
            var config = Configuration;
            config.Servers.Clear ();
            if (!File.Exists (path)) {
                Logger.WriteLine ("No settings file at " + path + ", using the defaults", Logger.Severity.Warning);
                config.Servers.Add (new Configuration.Server ());
                return;
            }
            var document = new TomlParser ().Parse (File.ReadAllText (path));
            config.AutoStartServers = GetBoolean (document, "auto_start", config.AutoStartServers);
            config.AutoAcceptConnections = GetBoolean (document, "auto_accept_connections", config.AutoAcceptConnections);
            config.ConfirmRemoveClient = GetBoolean (document, "confirm_remove_client", config.ConfirmRemoveClient);
            config.PauseServerWithGame = GetBoolean (document, "pause_server_with_game", config.PauseServerWithGame);
            config.OneRPCPerUpdate = GetBoolean (document, "one_rpc_per_update", config.OneRPCPerUpdate);
            config.MaxTimePerUpdate = GetMicroseconds (document, "max_time_per_update", config.MaxTimePerUpdate);
            config.AdaptiveRateControl = GetBoolean (document, "adaptive_rate_control", config.AdaptiveRateControl);
            config.BlockingRecv = GetBoolean (document, "blocking_recv", config.BlockingRecv);
            config.RecvTimeout = GetMicroseconds (document, "recv_timeout", config.RecvTimeout);
            config.TickHoldTimeout = GetMicroseconds (document, "tick_hold_timeout", config.TickHoldTimeout);
            config.VerboseErrors = GetBoolean (document, "verbose_errors", config.VerboseErrors);
            config.DebugLogging = GetBoolean (document, "debug_logging", config.DebugLogging);
            config.MainWindowVisible = GetBoolean (document, "main_window_visible", config.MainWindowVisible);
            config.InfoWindowVisible = GetBoolean (document, "info_window_visible", config.InfoWindowVisible);
            config.MainWindowPosition = GetRect (document, "main_window_rect", config.MainWindowPosition);
            config.InfoWindowPosition = GetRect (document, "info_window_rect", config.InfoWindowPosition);
            if (document.ContainsKey ("servers")) {
                foreach (var value in document.GetArray ("servers")) {
                    if (value is TomlTable table)
                        config.Servers.Add (ParseServer (table));
                }
            }
            if (config.Servers.Count == 0)
                config.Servers.Add (new Configuration.Server ());
        }

        static bool GetBoolean (TomlTable table, string key, bool defaultValue)
        {
            return table.ContainsKey (key) ? table.GetBoolean (key) : defaultValue;
        }

        static uint GetMicroseconds (TomlTable table, string key, uint defaultValue)
        {
            return table.ContainsKey (key) ? checked ((uint)table.GetLong (key)) : defaultValue;
        }

        // A window's (x, y, width, height) in pixels
        static Tuple<float, float, float, float> GetRect (TomlTable table, string key, Tuple<float, float, float, float> defaultValue)
        {
            if (!table.ContainsKey (key))
                return defaultValue;
            var values = table.GetArray (key).Select (x => float.Parse (x.StringValue, CultureInfo.InvariantCulture)).ToArray ();
            return values.Length == 4 ? Tuple.Create (values [0], values [1], values [2], values [3]) : defaultValue;
        }

        static Configuration.Server ParseServer (TomlTable table)
        {
            var server = new Configuration.Server ();
            var defaults = server.Settings;
            server.Settings = new Dictionary<string, string> ();
            foreach (var entry in table) {
                switch (entry.Key) {
                case "id":
                    server.Id = new Guid (entry.Value.StringValue);
                    break;
                case "name":
                    server.Name = entry.Value.StringValue;
                    break;
                case "protocol":
                    server.Protocol = Enum.Parse<Protocol> (entry.Value.StringValue);
                    break;
                default:
                    // Protocol settings, keyed as Configuration.Server.Create expects
                    server.Settings [entry.Key] = entry.Value.StringValue;
                    break;
                }
            }
            // The default address and ports, for a TCP or WebSockets server that leaves them out
            if (server.Protocol == Protocol.ProtocolBuffersOverTCP || server.Protocol == Protocol.ProtocolBuffersOverWebsockets) {
                foreach (var setting in defaults)
                    server.Settings.TryAdd (setting.Key, setting.Value);
            }
            return server;
        }

        public void Save ()
        {
            var config = Configuration;
            var toml = new StringBuilder ();
            toml.Append ("# kRPC for KSA server settings, also set from the kRPC window in game.\n\n");
            AppendValue (toml, "auto_start", config.AutoStartServers);
            AppendValue (toml, "auto_accept_connections", config.AutoAcceptConnections);
            AppendValue (toml, "confirm_remove_client", config.ConfirmRemoveClient);
            AppendValue (toml, "pause_server_with_game", config.PauseServerWithGame);
            toml.Append ('\n');
            toml.Append ("# Rate control. Times are in microseconds\n");
            AppendValue (toml, "one_rpc_per_update", config.OneRPCPerUpdate);
            AppendValue (toml, "max_time_per_update", config.MaxTimePerUpdate);
            AppendValue (toml, "adaptive_rate_control", config.AdaptiveRateControl);
            AppendValue (toml, "blocking_recv", config.BlockingRecv);
            AppendValue (toml, "recv_timeout", config.RecvTimeout);
            AppendValue (toml, "tick_hold_timeout", config.TickHoldTimeout);
            toml.Append ('\n');
            AppendValue (toml, "verbose_errors", config.VerboseErrors);
            AppendValue (toml, "debug_logging", config.DebugLogging);
            AppendValue (toml, "main_window_visible", config.MainWindowVisible);
            AppendValue (toml, "info_window_visible", config.InfoWindowVisible);
            toml.Append ("# Window (x, y, width, height) in pixels, with a zero width for the default\n");
            AppendValue (toml, "main_window_rect", config.MainWindowPosition);
            AppendValue (toml, "info_window_rect", config.InfoWindowPosition);
            foreach (var server in config.Servers) {
                toml.Append ("\n[[servers]]\n");
                AppendValue (toml, "id", server.Id.ToString ());
                AppendValue (toml, "name", server.Name);
                toml.Append ("# ProtocolBuffersOverTCP, ProtocolBuffersOverWebsockets, ProtocolBuffersOverLocalSocket\n");
                toml.Append ("# or ProtocolBuffersOverSerialIO\n");
                AppendValue (toml, "protocol", server.Protocol.ToString ());
                foreach (var setting in server.Settings) {
                    // Ports and the like are numbers, as in the shipped file
                    if (long.TryParse (setting.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                        AppendValue (toml, setting.Key, number);
                    else
                        AppendValue (toml, setting.Key, setting.Value);
                }
            }
            try {
                File.WriteAllText (path, toml.ToString ());
            } catch (IOException e) {
                Logger.WriteLine ("Failed to save settings to " + path + ": " + e.Message, Logger.Severity.Error);
            } catch (UnauthorizedAccessException e) {
                Logger.WriteLine ("Failed to save settings to " + path + ": " + e.Message, Logger.Severity.Error);
            }
        }

        static void AppendValue (StringBuilder toml, string key, bool value)
        {
            toml.Append (key).Append (" = ").Append (value ? "true" : "false").Append ('\n');
        }

        static void AppendValue (StringBuilder toml, string key, Tuple<float, float, float, float> value)
        {
            var values = new [] { value.Item1, value.Item2, value.Item3, value.Item4 };
            toml.Append (key).Append (" = [")
                .Append (string.Join (", ", values.Select (x => ((long)x).ToString (CultureInfo.InvariantCulture))))
                .Append ("]\n");
        }

        static void AppendValue (StringBuilder toml, string key, long value)
        {
            toml.Append (key).Append (" = ").Append (value.ToString (CultureInfo.InvariantCulture)).Append ('\n');
        }

        static void AppendValue (StringBuilder toml, string key, string value)
        {
            toml.Append (key).Append (" = \"");
            foreach (var c in value) {
                if (c == '"' || c == '\\')
                    toml.Append ('\\').Append (c);
                else if (c < ' ')
                    toml.Append ("\\u").Append (((int)c).ToString ("X4", CultureInfo.InvariantCulture));
                else
                    toml.Append (c);
            }
            toml.Append ("\"\n");
        }
    }
}
