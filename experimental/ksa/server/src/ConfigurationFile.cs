using System;
using System.Collections.Generic;
using System.IO;
using KRPC.Server;
using Tomlet;
using Tomlet.Models;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// Server settings, read from settings.toml in the mod folder.
    /// </summary>
    sealed class ConfigurationFile
    {
        public bool AutoStartServers { get; private set; } = true;

        public bool AutoAcceptConnections { get; private set; } = true;

        public IList<Configuration.Server> Servers { get; } = new List<Configuration.Server> ();

        public static ConfigurationFile Load (string path)
        {
            var config = new ConfigurationFile ();
            if (!File.Exists (path)) {
                Logger.WriteLine ("No settings file at " + path + ", using the defaults", Logger.Severity.Warning);
                config.Servers.Add (new Configuration.Server ());
                return config;
            }
            var document = new TomlParser ().Parse (File.ReadAllText (path));
            if (document.ContainsKey ("auto_start"))
                config.AutoStartServers = document.GetBoolean ("auto_start");
            if (document.ContainsKey ("auto_accept_connections"))
                config.AutoAcceptConnections = document.GetBoolean ("auto_accept_connections");
            if (document.ContainsKey ("servers")) {
                foreach (var value in document.GetArray ("servers")) {
                    if (value is TomlTable table)
                        config.Servers.Add (ParseServer (table));
                }
            }
            if (config.Servers.Count == 0)
                config.Servers.Add (new Configuration.Server ());
            return config;
        }

        static Configuration.Server ParseServer (TomlTable table)
        {
            var server = new Configuration.Server ();
            foreach (var entry in table) {
                switch (entry.Key) {
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
            return server;
        }
    }
}
