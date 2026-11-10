using System;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using RealAntennasAPI = KRPC.SpaceCenter.ExternalAPI.RealAntennas;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using ObjectNotLoadedException = KRPC.Service.KRPC.ObjectNotLoadedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a communication link between two nodes in the network.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CommLink : Equatable<CommLink>, IGameObjectState
    {
        // A link is a hop in one vessel's control path, identified by the two nodes it
        // joins. The game builds the path again, out of new link objects, every time it
        // works out the vessel's connection, so a link object stands for one hop at one
        // moment
        readonly Guid vesselId;
        readonly CommNet.CommNode start;
        readonly CommNet.CommNode end;

        /// <summary>
        /// Construct from a KSP CommLink object, and the vessel whose control path it is
        /// a hop in.
        /// </summary>
        public CommLink (Guid vessel, CommNet.CommLink link)
        {
            if (ReferenceEquals (link, null))
                throw new ArgumentNullException (nameof (link));
            vesselId = vessel;
            start = link.start;
            end = link.end;
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
        /// The state of the link. It is live while the vessel's control path runs through
        /// the two nodes, and destroyed once the vessel is destroyed. A path that drops the
        /// hop leaves the link dormant, and takes it again once the two nodes are in range
        /// of each other.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = FlightGlobalsExtensions.VesselState (vesselId)
                    .LeastAlive (NodeState (start))
                    .LeastAlive (NodeState (end));
                if (state != GameObjectState.Live)
                    return state;
                return Find () != null ? GameObjectState.Live : GameObjectState.Dormant;
            }
        }

        /// <summary>
        /// The state of the object a node hangs off. The game offers no identifier for a
        /// node, so the transform it was built with is what can be read. The game tears
        /// that transform down with the vessel, and a node built without one, such as a
        /// ground station, stays live.
        /// </summary>
        static GameObjectState NodeState (CommNet.CommNode node)
        {
            var transform = node.transform;
            if (ReferenceEquals (transform, null))
                return GameObjectState.Live;
            return transform != null ? GameObjectState.Live : GameObjectState.Destroyed;
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
                    ReferenceEquals (link.start, start) &&
                    ReferenceEquals (link.end, end))
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
            return new ObjectNotLoadedException (
                "The comm link is not part of the vessel's control path at the moment. " +
                "It can be used again if the path takes it again.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (CommLink other)
        {
            return !ReferenceEquals (other, null) && vesselId == other.vesselId &&
            ReferenceEquals (start, other.start) && ReferenceEquals (end, other.end);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The nodes' identity hashes, so that nothing the game does to a node changes
            // the code while a client holds this object
            return Hash.Of (vesselId)
                .And (RuntimeHelpers.GetHashCode (start))
                .And (RuntimeHelpers.GetHashCode (end));
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
            get { return new CommNode (start); }
        }

        /// <summary>
        /// End point of the link.
        /// </summary>
        [KRPCProperty]
        public CommNode End {
            get { return new CommNode (end); }
        }

        /// <summary>
        /// The RealAntennas link. Throws if RealAntennas is not available. A link's
        /// forward direction is from <see cref="Start"/> to <see cref="End"/>.
        /// </summary>
        CommNet.CommLink RealAntennasLink {
            get {
                var link = InternalLink;
                if (!RealAntennasAPI.IsLink (link))
                    throw new InvalidOperationException ("RealAntennas is not available");
                return link;
            }
        }

        /// <summary>
        /// The data rate from <see cref="Start"/> to <see cref="End"/>, in bits per second.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty]
        public double ForwardDataRate {
            get { return RealAntennasAPI.ForwardDataRate (RealAntennasLink); }
        }

        /// <summary>
        /// The data rate from <see cref="End"/> to <see cref="Start"/>, in bits per second.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty]
        public double ReverseDataRate {
            get { return RealAntennasAPI.ReverseDataRate (RealAntennasLink); }
        }

        /// <summary>
        /// The antenna at <see cref="Start"/> that transmits over the link, or <c>null</c> if
        /// <see cref="Start"/> is a ground station or an unloaded vessel.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Parts.Antenna StartAntenna {
            get { return ToAntenna (RealAntennasAPI.ForwardTransmitter (RealAntennasLink)); }
        }

        /// <summary>
        /// The antenna at <see cref="End"/> that receives over the link, or <c>null</c> if
        /// <see cref="End"/> is a ground station or an unloaded vessel.
        /// </summary>
        /// <remarks>
        /// Requires RealAntennas.
        /// </remarks>
        [KRPCProperty (Nullable = true)]
        public Parts.Antenna EndAntenna {
            get { return ToAntenna (RealAntennasAPI.ForwardReceiver (RealAntennasLink)); }
        }

        static Parts.Antenna ToAntenna (object antenna)
        {
            var module = RealAntennasAPI.Module (antenna) as ModuleDataTransmitter;
            return module == null ? null : new Parts.Antenna (new Parts.Part (module.part), module);
        }
    }
}
