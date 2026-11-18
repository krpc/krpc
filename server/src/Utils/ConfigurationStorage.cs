using System.IO;

namespace KRPC.Utils
{
    abstract class ConfigurationStorage : ConfigurationStorageNode
    {
        readonly string path;
        readonly string nodeName;

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
        /// </summary>
        public bool Load ()
        {
            if (!FileExists)
                return false;
            ConfigNode node = ConfigNode.Load (path);
            if (node == null || !node.HasNode (nodeName)) {
                // Keep a copy of a file with no settings in it, as the next save replaces it
                if (File.ReadAllText (path).Trim ().Length > 0) {
                    Logger.WriteLine ("Failed to read settings from " + path + ", copied it to " + path + ".bak", Logger.Severity.Warning);
                    File.Copy (path, path + ".bak", true);
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
            ConfigNode node = AsConfigNode;
            var clsNode = new ConfigNode (nodeName);
            clsNode.AddNode (node);
            Directory.CreateDirectory (Path.GetDirectoryName (path));
            clsNode.Save (path);
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
