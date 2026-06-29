using System;
using System.IO;
using UnityEngine;

namespace TestingTools
{
    sealed class TestingToolsOptions
    {
        const string GameArgument = "--krpc-auto-load-game=";
        const string SaveArgument = "--krpc-auto-load-save=";
        const string VesselArgument = "--krpc-auto-load-vessel=";
        const string CraftArgument = "--krpc-auto-load-craft=";
        const string CraftDirectoryArgument = "--krpc-auto-load-craft-directory=";
        const string CraftFixtureDirectoryArgument = "--krpc-auto-load-craft-fixture-dir=";
        const string LaunchSiteArgument = "--krpc-auto-load-launch-site=";

        static TestingToolsOptions instance;

        TestingToolsOptions()
        {
            Game = "default";
            Save = "persistent";
        }

        public static TestingToolsOptions Instance {
            get {
                if (instance == null)
                    instance = Parse(Environment.GetCommandLineArgs());
                return instance;
            }
        }

        public string Game { get; private set; }
        public string Save { get; private set; }
        public int? Vessel { get; private set; }
        public string Craft { get; private set; }
        public string CraftDirectory { get; private set; }
        public string CraftFixtureDirectory { get; private set; }
        public string LaunchSite { get; private set; }

        bool craftDirectorySpecified;
        bool launchSiteSpecified;

        public bool HasCraft {
            get { return !string.IsNullOrEmpty(Craft); }
        }

        static TestingToolsOptions Parse(string[] args)
        {
            var options = new TestingToolsOptions();
            foreach (var arg in args) {
                string value;
                if (TryGetArgumentValue(arg, GameArgument, out value))
                    options.Game = value;
                else if (TryGetArgumentValue(arg, SaveArgument, out value))
                    options.Save = value;
                else if (TryGetArgumentValue(arg, VesselArgument, out value))
                    options.ParseVessel(value);
                else if (TryGetArgumentValue(arg, CraftArgument, out value))
                    options.Craft = NormalizeCraftName(value);
                else if (TryGetArgumentValue(arg, CraftDirectoryArgument, out value))
                    options.ParseCraftDirectory(value);
                else if (TryGetArgumentValue(arg, CraftFixtureDirectoryArgument, out value))
                    options.CraftFixtureDirectory = value;
                else if (TryGetArgumentValue(arg, LaunchSiteArgument, out value)) {
                    options.LaunchSite = value;
                    options.launchSiteSpecified = true;
                }
            }
            options.ResolveCraftDefaults();
            return options;
        }

        void ParseVessel(string value)
        {
            int vessel;
            if (int.TryParse(value, out vessel) && vessel >= 0) {
                Vessel = vessel;
                return;
            }
            Debug.LogWarning("[kRPC testing tools]: Ignoring invalid vessel index '" + value + "'");
        }

        void ParseCraftDirectory(string value)
        {
            value = value.ToUpperInvariant();
            if (value == "VAB" || value == "SPH") {
                CraftDirectory = value;
                craftDirectorySpecified = true;
                return;
            }
            Debug.LogWarning("[kRPC testing tools]: Ignoring invalid craft directory '" + value + "'");
        }

        void ResolveCraftDefaults()
        {
            if (!craftDirectorySpecified)
                CraftDirectory = string.Equals(LaunchSite, "Runway", StringComparison.OrdinalIgnoreCase) ? "SPH" : "VAB";
            if (!launchSiteSpecified)
                LaunchSite = CraftDirectory == "SPH" ? "Runway" : "LaunchPad";
        }

        static bool TryGetArgumentValue(string arg, string prefix, out string value)
        {
            value = null;
            if (!arg.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            value = TrimQuotes(arg.Substring(prefix.Length).Trim());
            return !string.IsNullOrEmpty(value);
        }

        static string NormalizeCraftName(string value)
        {
            if (value.EndsWith(".craft", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(0, value.Length - ".craft".Length);
            return Path.GetFileName(value);
        }

        static string TrimQuotes(string value)
        {
            if (value.Length >= 2 &&
                ((value [0] == '"' && value [value.Length - 1] == '"') ||
                 (value [0] == '\'' && value [value.Length - 1] == '\'')))
                return value.Substring(1, value.Length - 2);
            return value;
        }
    }
}
