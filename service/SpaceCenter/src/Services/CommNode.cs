using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a communication node in the network. For example, a vessel or the KSC.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CommNode : Equatable<CommNode>
    {
        /// <summary>
        /// Construct from a KSP CommNode object.
        /// </summary>
        public CommNode (CommNet.CommNode node)
        {
            if (ReferenceEquals (node, null))
                throw new ArgumentNullException (nameof (node));
            InternalNode = node;
        }

        /// <summary>
        /// KSP CommNode object
        /// </summary>
        public CommNet.CommNode InternalNode { get; private set; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CommNode other)
        {
            return !ReferenceEquals (other, null) && InternalNode == other.InternalNode;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalNode.GetHashCode ();
        }

        /// <summary>
        /// Name of the communication node.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalNode.name; }
        }

        /// <summary>
        /// Whether the communication node is on Kerbin.
        /// </summary>
        [KRPCProperty]
        public bool IsHome {
            get { return InternalNode.isHome; }
        }

        /// <summary>
        /// Whether the communication node is a control point, for example a manned vessel.
        /// </summary>
        [KRPCProperty]
        public bool IsControlPoint {
            get { return InternalNode.isControlSource; }
        }

        /// <summary>
        /// The KSP vessel this node belongs to, or null if it is not a vessel's node.
        /// A vessel's node is constructed with the vessel's transform, so the vessel
        /// component can be read straight off it without scanning every vessel in the
        /// game. The vessel's connection is checked to confirm the node is its current
        /// one, so a stale node from a rebuilt network is not treated as the vessel's.
        /// </summary>
        global::Vessel InternalVessel {
            get {
                var transform = InternalNode.transform;
                if (transform == null)
                    return null;
                var vessel = transform.GetComponent<global::Vessel> ();
                if (vessel == null)
                    return null;
                var connection = vessel.Connection;
                return connection != null && connection.Comm == InternalNode ? vessel : null;
            }
        }

        /// <summary>
        /// Whether the communication node is a vessel.
        /// </summary>
        [KRPCProperty]
        public bool IsVessel {
            get { return InternalVessel != null; }
        }

        /// <summary>
        /// The vessel for this communication node.
        /// </summary>
        [KRPCProperty]
        public Vessel Vessel {
            get {
                var vessel = InternalVessel;
                if (vessel == null)
                    throw new InvalidOperationException ("Communication node is not part of a vessel");
                return new Vessel (vessel);
            }
        }
    }
}
