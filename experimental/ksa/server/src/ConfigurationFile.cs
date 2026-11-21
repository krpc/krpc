using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KRPC.Server;
using Tomlet;
using Tomlet.Exceptions;
using Tomlet.Models;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// Server settings, stored in kRPC/settings.toml in the game's user folder, beside the game's
    /// own settings, so they apply to every save and survive updates of the mod. Loads them into
    /// <see cref="Configuration.Instance"/>, and saves them from it.
    /// </summary>
    sealed class ConfigurationFile
    {
        /// <summary>
        /// The protocol of a gRPC server. The server core has no gRPC protocol, so the value is
        /// outside its enumeration, and names the server only in logs.
        /// </summary>
        public const Protocol GrpcProtocol = (Protocol)100;

        readonly string path;
        // Whether the file could not be read when the settings loaded, so saving would replace
        // the player's settings with the defaults
        bool keepFile;

        public ConfigurationFile (string path)
        {
            this.path = path;
            // Defaults that differ from the core's, for a game with no other way to start the server
            Configuration.AutoStartServers = true;
            Configuration.AutoAcceptConnections = true;
            Configuration.MainWindowVisible = false;
        }

        /// <summary>
        /// The settings file in the game's user folder.
        /// </summary>
        public static string DefaultPath {
            get { return Path.Combine (global::KSA.Constants.DocumentsFolderPath, "kRPC", "settings.toml"); }
        }

        public Configuration Configuration {
            get { return Configuration.Instance; }
        }

        /// <summary>
        /// The game version the player chose not to be warned about, or null for none.
        /// </summary>
        public string IgnoredGameVersion { get; set; }

        /// <summary>
        /// Load the settings, then save them. A missing file, or one that does not parse, gives
        /// the defaults. A setting or server that does not parse takes its default, or is left
        /// out. Saving writes the settings in use over the file. A file that does not parse is
        /// first copied to settings.toml.bak. A file that could not be read is kept, and the
        /// settings are not saved until the next load.
        /// </summary>
        public void Load ()
        {
            var config = Configuration;
            config.Servers.Clear ();
            var document = Read (out keepFile);
            if (document != null)
                Load (document);
            if (config.Servers.Count == 0) {
                foreach (var server in Configuration.DefaultServers ())
                    config.Servers.Add (server);
            }
            Save ();
        }

        // The settings document, or null for the defaults. Whether the file is to be kept: it could
        // not be read, such as while another program holds it open, or not be backed up.
        TomlDocument Read (out bool unreadable)
        {
            unreadable = false;
            if (!File.Exists (path)) {
                Logger.WriteLine ("No settings file at " + path + ", creating one with the defaults");
                return null;
            }
            string text;
            try {
                text = File.ReadAllText (path);
            } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                Logger.WriteLine ("Failed to read the settings file " + path + ", using the defaults and keeping the file: " + e.Message, Logger.Severity.Warning);
                unreadable = true;
                return null;
            }
            try {
                return new TomlParser ().Parse (text);
            } catch (TomlException e) {
                try {
                    File.Copy (path, path + ".bak", true);
                    Logger.WriteLine ("Failed to parse the settings file " + path + ", copied it to " + path + ".bak and replacing it with the defaults: " + e.Message, Logger.Severity.Warning);
                } catch (Exception copyError) when (copyError is IOException || copyError is UnauthorizedAccessException) {
                    Logger.WriteLine ("Failed to parse the settings file " + path + ", using the defaults and keeping the file, as it could not be copied: " + copyError.Message, Logger.Severity.Warning);
                    unreadable = true;
                }
                return null;
            }
        }

        void Load (TomlDocument document)
        {
            var config = Configuration;
            config.AutoStartServers = GetBoolean (document, "auto_start", config.AutoStartServers);
            config.AutoAcceptConnections = GetBoolean (document, "auto_accept_connections", config.AutoAcceptConnections);
            config.ConfirmRemoveClient = GetBoolean (document, "confirm_remove_client", config.ConfirmRemoveClient);
            config.PauseServerWithGame = GetBoolean (document, "pause_server_with_game", config.PauseServerWithGame);
            IgnoredGameVersion = Get (document, "ignored_ksa_version", null, x => ((TomlString)x).Value);
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
            var servers = Get<IEnumerable<TomlValue>> (document, "servers", Array.Empty<TomlValue> (), x => (TomlArray)x);
            foreach (var value in servers) {
                var server = value is TomlTable table ? ParseServer (table) : null;
                if (server != null)
                    config.Servers.Add (server);
                else
                    Logger.WriteLine ("Ignoring a server in the settings file that is not valid", Logger.Severity.Warning);
            }
        }

        // The value of a setting, or the default when it is missing or does not convert
        static T Get<T> (TomlTable table, string key, T defaultValue, Func<TomlValue, T> convert)
        {
            if (!table.ContainsKey (key))
                return defaultValue;
            try {
                return convert (table.GetValue (key));
            } catch (Exception e) when (e is InvalidCastException || e is FormatException || e is OverflowException) {
                Logger.WriteLine ("The setting " + key + " in the settings file is not valid, using the default", Logger.Severity.Warning);
                return defaultValue;
            }
        }

        static bool GetBoolean (TomlTable table, string key, bool defaultValue)
        {
            return Get (table, key, defaultValue, x => ((TomlBoolean)x).Value);
        }

        static uint GetMicroseconds (TomlTable table, string key, uint defaultValue)
        {
            return Get (table, key, defaultValue, x => checked ((uint)((TomlLong)x).Value));
        }

        // A window's (x, y, width, height) in pixels
        static Tuple<float, float, float, float> GetRect (TomlTable table, string key, Tuple<float, float, float, float> defaultValue)
        {
            return Get (table, key, defaultValue, x => {
                var values = ((TomlArray)x).Select (y => float.Parse (y.StringValue, CultureInfo.InvariantCulture)).ToArray ();
                if (values.Length != 4)
                    throw new FormatException ();
                return Tuple.Create (values [0], values [1], values [2], values [3]);
            });
        }

        // A server, or null when its protocol is missing or unknown. A missing or invalid id or
        // name takes a new one.
        static Configuration.Server ParseServer (TomlTable table)
        {
            Protocol protocol;
            var protocolName = Get<string> (table, "protocol", null, x => ((TomlString)x).Value);
            if (protocolName == "gRPC")
                protocol = GrpcProtocol;
            else if (protocolName == null || !Enum.TryParse (protocolName, out protocol) || !Enum.IsDefined (protocol))
                return null;
            var server = new Configuration.Server ();
            server.Protocol = protocol;
            server.Settings = new Dictionary<string, string> ();
            server.Id = Get (table, "id", server.Id, x => new Guid (((TomlString)x).Value));
            server.Name = Get (table, "name", server.Name, x => ((TomlString)x).Value);
            foreach (var entry in table) {
                if (entry.Key == "id" || entry.Key == "name" || entry.Key == "protocol")
                    continue;
                // Protocol settings, keyed as Configuration.Server.Create expects
                if (entry.Value is TomlString || entry.Value is TomlLong || entry.Value is TomlBoolean)
                    server.Settings [entry.Key] = entry.Value.StringValue;
                else
                    Logger.WriteLine ("The setting " + entry.Key + " of server " + server.Name + " in the settings file is not valid, leaving it out", Logger.Severity.Warning);
            }
            return server;
        }

        public void Save ()
        {
            if (keepFile)
                return;
            var config = Configuration;
            var toml = new StringBuilder ();
            toml.Append ("# kRPC for KSA server settings, also set from the kRPC window in game.\n\n");
            AppendValue (toml, "auto_start", config.AutoStartServers);
            AppendValue (toml, "auto_accept_connections", config.AutoAcceptConnections);
            AppendValue (toml, "confirm_remove_client", config.ConfirmRemoveClient);
            AppendValue (toml, "pause_server_with_game", config.PauseServerWithGame);
            if (IgnoredGameVersion != null) {
                toml.Append ("# The KSA version with no warning that kRPC was built for another version\n");
                AppendValue (toml, "ignored_ksa_version", IgnoredGameVersion);
            }
            toml.Append ('\n');
            toml.Append ("# Rate control. Times are in microseconds\n");
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
                toml.Append ("# ProtocolBuffersOverTCP, ProtocolBuffersOverWebsockets, ProtocolBuffersOverLocalSocket,\n");
                toml.Append ("# ProtocolBuffersOverSerialIO or gRPC\n");
                AppendValue (toml, "protocol", server.Protocol == GrpcProtocol ? "gRPC" : server.Protocol.ToString ());
                foreach (var setting in server.Settings) {
                    // Ports and the like are numbers, as in the shipped file
                    if (long.TryParse (setting.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                        AppendValue (toml, setting.Key, number);
                    else
                        AppendValue (toml, setting.Key, setting.Value);
                }
            }
            try {
                Directory.CreateDirectory (Path.GetDirectoryName (path));
                File.WriteAllText (path, toml.ToString ());
            } catch (IOException e) {
                Logger.WriteLine ("Failed to save settings to " + path + ": " + e.Message, Logger.Severity.Warning);
            } catch (UnauthorizedAccessException e) {
                Logger.WriteLine ("Failed to save settings to " + path + ": " + e.Message, Logger.Severity.Warning);
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
