using System;
using System.IO;

namespace KRPC.Utils
{
    abstract class ConfigurationStorage : ConfigurationStorageNode
    {
        readonly string path;
        readonly string nodeName;
        // Whether the file could not be read when the settings loaded, so saving would replace
        // the player's settings with the defaults
        bool keepFile;

        /// <summary>
        /// Create a configuration object with default values. Call Load() to load from the file.
        /// The file path is relative to the directory containing this assembly.
        /// </summary>
        protected ConfigurationStorage (string filePath, string name)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            var dir = Path.GetDirectoryName (assembly).Replace ('\\', '/');
            path = dir + "/" + filePath;
            nodeName = name;
        }

        /// <summary>
        /// Load settings from the underlying storage. Returns whether the storage held settings.
        /// A file that does not parse is copied to a .bak file, as the next save replaces it. A
        /// file that cannot be read, such as while another program holds it open, is kept, and
        /// the settings are not saved until a load reads it.
        /// </summary>
        public bool Load ()
        {
            keepFile = false;
            if (!FileExists)
                return false;
            string text;
            try {
                text = File.ReadAllText (path);
            } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                Logger.WriteLine ("Failed to read settings from " + path + ", using the defaults and keeping the file: " + e.Message, Logger.Severity.Warning);
                keepFile = true;
                return false;
            }
            ConfigNode node = ConfigNode.Parse (text);
            if (node == null || !node.HasNode (nodeName)) {
                if (text.Trim ().Length > 0) {
                    try {
                        File.Copy (path, path + ".bak", true);
                        Logger.WriteLine ("Failed to parse settings from " + path + ", copied it to " + path + ".bak and replacing it with the defaults", Logger.Severity.Warning);
                    } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                        Logger.WriteLine ("Failed to parse settings from " + path + ", using the defaults and keeping the file, as it could not be copied: " + e.Message, Logger.Severity.Warning);
                        keepFile = true;
                    }
                }
                return false;
            }
            ConfigNode.LoadObjectFromConfig (this, node.GetNode (nodeName));
            return true;
        }

        /// <summary>
        /// Save settings to the underlying storage
        /// </summary>
        public void Save ()
        {
            if (keepFile)
                return;
            ConfigNode node = AsConfigNode;
            var clsNode = new ConfigNode (nodeName);
            clsNode.AddNode (node);
            try {
                Directory.CreateDirectory (Path.GetDirectoryName (path));
                clsNode.Save (path);
            } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
                Logger.WriteLine ("Failed to save settings to " + path + ": " + e.Message, Logger.Severity.Warning);
            }
        }

        bool FileExists {
            get { return File.Exists (path); }
        }

        ConfigNode AsConfigNode {
            get {
                var node = new ConfigNode (nodeName);
                return ConfigNode.CreateConfigFromObject (this, node);
            }
        }
    }
}
