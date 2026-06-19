using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Represents a configuration node, as found in a part's config file. A node has a
    /// name, a set of named values and a set of child nodes. This is used to access the
    /// static configuration of a part or part module, for example via
    /// <see cref="Part.Config"/> and <see cref="Module.Config"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ConfigNode : Equatable<ConfigNode>
    {
        readonly global::ConfigNode node;

        internal ConfigNode (global::ConfigNode configNode)
        {
            if (ReferenceEquals (configNode, null))
                throw new ArgumentNullException (nameof (configNode));
            node = configNode;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ConfigNode other)
        {
            return !ReferenceEquals (other, null) && node == other.node;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return node.GetHashCode ();
        }

        /// <summary>
        /// The name of the configuration node. For example "MODULE".
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return node.name; }
        }

        /// <summary>
        /// The values stored in the node, as a dictionary mapping names to values.
        /// </summary>
        /// <remarks>
        /// If a name appears more than once, only the first value is included. Use
        /// <see cref="GetValues"/> to get all of the values with a given name.
        /// </remarks>
        [KRPCProperty]
        public IDictionary<string,string> Values {
            get {
                var result = new Dictionary<string,string> ();
                for (int i = 0; i < node.values.Count; i++) {
                    var value = node.values [i];
                    if (!result.ContainsKey (value.name))
                        result.Add (value.name, value.value);
                }
                return result;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the node has a value with the given name.
        /// </summary>
        /// <param name="name">Name of the value.</param>
        [KRPCMethod]
        public bool HasValue (string name)
        {
            return node.HasValue (name);
        }

        /// <summary>
        /// Returns the value with the given name. If there is more than one value with
        /// the given name, the first is returned. Throws an exception if there is no
        /// value with the given name.
        /// </summary>
        /// <param name="name">Name of the value.</param>
        [KRPCMethod]
        public string GetValue (string name)
        {
            if (!node.HasValue (name))
                throw new InvalidOperationException ("Config node does not have a value with the given name");
            return node.GetValue (name);
        }

        /// <summary>
        /// Returns all of the values with the given name, as a list.
        /// </summary>
        /// <param name="name">Name of the values.</param>
        [KRPCMethod]
        public IList<string> GetValues (string name)
        {
            return new List<string> (node.GetValues (name));
        }

        /// <summary>
        /// The child nodes contained in this node.
        /// </summary>
        [KRPCProperty]
        public IList<ConfigNode> Nodes {
            get {
                var result = new List<ConfigNode> ();
                for (int i = 0; i < node.nodes.Count; i++)
                    result.Add (new ConfigNode (node.nodes [i]));
                return result;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the node has a child node with the given name.
        /// </summary>
        /// <param name="name">Name of the child node.</param>
        [KRPCMethod]
        public bool HasNode (string name)
        {
            return node.HasNode (name);
        }

        /// <summary>
        /// Returns the child node with the given name. If there is more than one child
        /// node with the given name, the first is returned. Throws an exception if there
        /// is no child node with the given name.
        /// </summary>
        /// <param name="name">Name of the child node.</param>
        [KRPCMethod]
        public ConfigNode GetNode (string name)
        {
            if (!node.HasNode (name))
                throw new InvalidOperationException ("Config node does not have a child node with the given name");
            return new ConfigNode (node.GetNode (name));
        }

        /// <summary>
        /// Returns all of the child nodes with the given name, as a list.
        /// </summary>
        /// <param name="name">Name of the child nodes.</param>
        [KRPCMethod]
        public IList<ConfigNode> GetNodes (string name)
        {
            var result = new List<ConfigNode> ();
            foreach (var child in node.GetNodes (name))
                result.Add (new ConfigNode (child));
            return result;
        }
    }
}
