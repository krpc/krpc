using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace KRPC.Utils
{
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
    abstract class ConfigurationStorage : ConfigurationStorageNode
    {
        readonly string path;

        /// <summary>
        /// Create a configuration object with default values. Call Load() to load from the file.
        /// The file path is relative to the directory containing this assembly.
        /// </summary>
        protected ConfigurationStorage (string filePath)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            var dir = Path.GetDirectoryName (assembly).Replace ('\\', '/');
            path = dir + "/" + filePath;
            Logger.WriteLine ("Configuration file path " + path);
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
                var name = GetType ().Name;
                if (node != null && node.HasNode (name))
                    ConfigNode.LoadObjectFromConfig (this, node.GetNode (name));
            }
        }

        /// <summary>
        /// Save settings to the underlying storage
        /// </summary>
        public void Save ()
        {
            ConfigNode node = AsConfigNode;
            var clsNode = new ConfigNode (GetType ().Name);
            clsNode.AddNode (node);
            clsNode.Save (path);
        }

        bool FileExists {
            get { return File.Exists (path); }
        }

        ConfigNode AsConfigNode {
            get {
                var node = new ConfigNode (GetType ().Name);
                return ConfigNode.CreateConfigFromObject (this, node);
            }
        }
    }
}
