using System;
using System.IO;

namespace KRPC.Utils
{
    abstract class ConfigurationStorage : ConfigurationStorageNode
    {
        string filePath;

        /// <summary>
        /// Create a configuration object with default values. Call Load() to load from the file.
        /// The file path is relative to the directory containing this assembly.
        /// </summary>
        protected ConfigurationStorage (string filePath)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            var dir = Path.GetDirectoryName (assembly).Replace ("\\", "/");
            this.filePath = dir + "/" + filePath;
            Logger.WriteLine ("Configuration file path " + this.filePath);
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
                ConfigNode node = ConfigNode.Load (filePath);
                if (node != null && node.HasNode (GetType ().Name))
                    ConfigNode.LoadObjectFromConfig (this, node.GetNode (GetType ().Name));
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
            clsNode.Save (filePath);
        }

        bool FileExists {
            get { return File.Exists (filePath); }
        }

        ConfigNode AsConfigNode {
            get {
                var node = new ConfigNode (GetType ().Name);
                return ConfigNode.CreateConfigFromObject (this, node);
            }
        }
    }
}

