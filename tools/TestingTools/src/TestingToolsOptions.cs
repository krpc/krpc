using System;
using System.IO;
using UnityEngine;

namespace TestingTools
{
    sealed class TestingToolsOptions
    {
        // All auto-load arguments share this prefix. Auto-load is enabled by the
        // presence of any argument with this prefix, so future TestingTools
        // arguments that use a different prefix will not trigger a load.
        const string AutoLoadPrefix = "--krpc-auto-load-";
        const string GameArgument = AutoLoadPrefix + "game=";
        const string SaveArgument = AutoLoadPrefix + "save=";
        const string VesselArgument = AutoLoadPrefix + "vessel=";
        const string CraftArgument = AutoLoadPrefix + "craft=";
        const string CraftDirectoryArgument = AutoLoadPrefix + "craft-directory=";
        const string CraftFixtureDirectoryArgument = AutoLoadPrefix + "craft-fixture-dir=";
        const string LaunchSiteArgument = AutoLoadPrefix + "launch-site=";

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

        /// <summary>
        /// Whether any auto-load argument was supplied. When false, no save is
        /// loaded and KSP is left at the main menu.
        /// </summary>
        public bool AutoLoadRequested { get; private set; }

        bool craftDirectorySpecified;
        bool launchSiteSpecified;

        public bool HasCraft {
            get { return !string.IsNullOrEmpty(Craft); }
        }

        static TestingToolsOptions Parse(string[] args)
        {
            var options = new TestingToolsOptions();
            foreach (var arg in args) {
                // Any auto-load argument requests a load. Keyed off the shared
                // prefix, not the individual arguments, so unrelated TestingTools
                // arguments added later cannot trigger a load by accident.
                if (arg.StartsWith(AutoLoadPrefix, StringComparison.Ordinal))
                    options.AutoLoadRequested = true;

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
