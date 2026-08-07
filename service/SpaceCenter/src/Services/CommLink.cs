using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a communication node in the network. For example, a vessel or the KSC.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CommLink : Equatable<CommLink>, IGameObjectState
    {
        // A link is a hop in one vessel's control path, and the two nodes it joins are what
        // identify it: the game builds the path again, out of new link objects, every time
        // it works out the vessel's connection, so a link object stands for the state of
        // one hop at one moment rather than for the hop itself.
        readonly Guid vesselId;
        readonly CommNode start;
        readonly CommNode end;

        /// <summary>
        /// Construct from a KSP CommLink object, and the vessel whose control path it is
        /// a hop in.
        /// </summary>
        public CommLink (Guid vessel, CommNet.CommLink link)
        {
            if (ReferenceEquals (link, null))
                throw new ArgumentNullException (nameof (link));
            vesselId = vessel;
            start = new CommNode (link.start);
            end = new CommNode (link.end);
        }

        /// <summary>
        /// KSP CommLink object, found again in the vessel's control path as it is now.
        /// </summary>
        public CommNet.CommLink InternalLink {
            get {
                var link = Find ();
                if (link == null)
                    throw NotResolvable ();
                return link;
            }
        }

        /// <summary>
        /// What the game holds for the link. It is live while the vessel's control path
        /// still runs through the two nodes, and destroyed once the vessel is gone. A path
        /// that no longer takes the hop has lost contact rather than destroyed anything,
        /// and takes it again when the two are in range of each other, so the link is
        /// dormant.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = FlightGlobalsExtensions.VesselState (vesselId)
                    .LeastAlive (start.GameObjectState)
                    .LeastAlive (end.GameObjectState);
                if (state != GameObjectState.Live)
                    return state;
                return Find () != null ? GameObjectState.Live : GameObjectState.Dormant;
            }
        }

        /// <summary>
        /// The hop between the two nodes in the vessel's control path as it is now, or null
        /// if the path does not take it.
        /// </summary>
        CommNet.CommLink Find ()
        {
            var vessel = FlightGlobalsExtensions.FindVesselById (vesselId);
            if (vessel == null)
                return null;
            var connection = vessel.Connection;
            var path = connection == null ? null : connection.ControlPath;
            if (path == null)
                return null;
            for (var i = 0; i < path.Count; i++) {
                var link = path [i];
                if (link != null &&
                    ReferenceEquals (link.start, start.InternalNode) &&
                    ReferenceEquals (link.end, end.InternalNode))
                    return link;
            }
            return null;
        }

        /// <summary>
        /// The error to raise when the vessel's control path no longer takes the hop, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The comm link no longer exists, as the vessel whose control path it " +
                    "was part of, or one of the nodes it joins, is gone.");
            return new InvalidOperationException (
                "The comm link is not part of the vessel's control path at the moment. " +
                "It can be used again if the path takes it again.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CommLink other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId &&
            start.Equals (other.start) && end.Equals (other.end);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode () ^ start.GetHashCode () ^ end.GetHashCode ();
        }

        /// <summary>
        /// The type of link.
        /// </summary>
        [KRPCProperty]
        public CommLinkType Type {
            get { return InternalLink.hopType.ToCommLinkType (); }
        }

        /// <summary>
        /// Signal strength of the link.
        /// </summary>
        [KRPCProperty]
        public double SignalStrength {
            get { return InternalLink.signalStrength; }
        }

        /// <summary>
        /// Start point of the link.
        /// </summary>
        [KRPCProperty]
        public CommNode Start {
            get { return start; }
        }

        /// <summary>
        /// Start point of the link.
        /// </summary>
        [KRPCProperty]
        public CommNode End {
            get { return end; }
        }
    }
}
