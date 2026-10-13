using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a communication node in the network. For example, a vessel or the KSC.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct CommNode
    {
        /// <summary>
        /// Construct from a KSP CommNode object.
        /// </summary>
        internal CommNode (CommNet.CommNode node) : this ()
        {
            if (ReferenceEquals (node, null))
                throw new ArgumentNullException (nameof (node));
            var vessel = VesselFor (node);
            Name = node.name;
            IsHome = node.isHome;
            IsControlPoint = node.isControlSource;
            IsVessel = vessel != null;
            Vessel = vessel == null ? null : new Vessel (vessel);
        }

        /// <summary>
        /// The KSP vessel the node belongs to, or null if it is not a vessel's node.
        /// A vessel's node is constructed with the vessel's transform, so the vessel
        /// component can be read straight off it without scanning every vessel in the
        /// game. The vessel's connection is checked to confirm the node is its current
        /// one, so a stale node from a rebuilt network is not treated as the vessel's.
        /// </summary>
        static global::Vessel VesselFor (CommNet.CommNode node)
        {
            var transform = node.transform;
            if (transform == null)
                return null;
            var vessel = transform.GetComponent<global::Vessel> ();
            if (vessel == null)
                return null;
            var connection = vessel.Connection;
            return connection != null && connection.Comm == node ? vessel : null;
        }

        /// <summary>
        /// Name of the communication node.
        /// </summary>
        [KRPCProperty]
        public string Name { get; set; }

        /// <summary>
        /// Whether the communication node is on Kerbin.
        /// </summary>
        [KRPCProperty]
        public bool IsHome { get; set; }

        /// <summary>
        /// Whether the communication node is a control point, for example a manned vessel.
        /// </summary>
        [KRPCProperty]
        public bool IsControlPoint { get; set; }

        /// <summary>
        /// Whether the communication node is a vessel.
        /// </summary>
        [KRPCProperty]
        public bool IsVessel { get; set; }

        /// <summary>
        /// The vessel for this communication node, or null if the node is not a vessel.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Vessel Vessel { get; set; }
    }
}
