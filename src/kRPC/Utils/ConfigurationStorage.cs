using System;
using System.IO;
using System.Net;
using KSP;

namespace KRPC.Utils
{
    abstract class ConfigurationStorage : ConfigurationStorageNode
    {
        private string filePath;

        /// <summary>
        /// Create a configuration object with default values. Call Load() to load from the file.
        /// The file path is relative to the directory containing this assembly.
        /// </summary>
        public ConfigurationStorage (string filePath)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly ().Location;
            var dir = Path.GetDirectoryName (assembly).Replace ("\\", "/");
            this.filePath = dir + "/" + filePath;
            Logger.WriteLine ("Configuration file path " + this.filePath);
        }

        private ConfigurationStorage() {
        }

        /// <summary>
        /// Load settings from the underlying storage
        /// </summary>
        public void Load() {
            if (FileExists) {
                ConfigNode node = ConfigNode.Load (filePath).GetNode (this.GetType ().Name);
                ConfigNode.LoadObjectFromConfig (this, node);
            }
        }

        /// <summary>
        /// Save settings to the underlying storage
        /// </summary>
        public void Save() {
            ConfigNode node = this.AsConfigNode;
            ConfigNode clsNode = new ConfigNode(this.GetType().Name);
            clsNode.AddNode(node);
            clsNode.Save(filePath);
        }

        private bool FileExists {
            get { return File.Exists (filePath); }
        }

        private ConfigNode AsConfigNode {
            get {
                ConfigNode node = new ConfigNode (this.GetType().Name);
                return ConfigNode.CreateConfigFromObject (this, node);
            }
        }
    }
}

