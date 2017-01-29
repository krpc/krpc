using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
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

        ConfigurationStorage ()
        {
        }

        /// <summary>
        /// Load settings from the underlying storage
        /// </summary>
        public void Load ()
        {
            if (FileExists) {
                ConfigNode node = ConfigNode.Load (path);
                if (node != null && node.HasNode (nodeName))
                    ConfigNode.LoadObjectFromConfig (this, node.GetNode (nodeName));
            }
        }

        /// <summary>
        /// Save settings to the underlying storage
        /// </summary>
        public void Save ()
        {
            ConfigNode node = AsConfigNode;
            var clsNode = new ConfigNode (nodeName);
            clsNode.AddNode (node);
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
