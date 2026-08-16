using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a waypoint. Can be created using <see cref="WaypointManager.AddWaypoint"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Waypoint : Equatable<Waypoint>, IGameObjectState
    {
        // The identifier the game gives a waypoint when it builds one. Nothing else names
        // a waypoint: its name is neither unique nor fixed, and its seed and index only
        // pick out the waypoints of a contract.
        readonly Guid navigationId;

        /// <summary>
        /// Create a waypoint object.
        /// </summary>
        internal Waypoint (double latitude, double longitude, double altitude, CelestialBody body, string name)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            // The waypoint is set up before the game is given it, so this works on it
            // directly: until the game has it, there is nothing for this object to find.
            // What each line does is what the property of the same name does.
            var waypoint = new FinePrint.Waypoint ();
            waypoint.name = name;
            waypoint.celestialName = body.Name;
            waypoint.id = "report";
            waypoint.seed = 1115;
            waypoint.latitude = latitude;
            waypoint.longitude = longitude;
            waypoint.altitude = altitude - body.BedrockHeight (latitude, longitude);
            waypoint.isOnSurface =
                Math.Abs (body.SurfaceHeight (latitude, longitude) - altitude) < 10;
            waypoint.isNavigatable = true;
            navigationId = waypoint.navigationId;
            FinePrint.WaypointManager.AddWaypoint (waypoint);
        }

        /// <summary>
        /// Create a waypoint object from a KSP waypoint.
        /// </summary>
        public Waypoint (FinePrint.Waypoint wp)
        {
            if (wp == null)
                throw new ArgumentNullException (nameof (wp));
            navigationId = wp.navigationId;
        }

        /// <summary>
        /// The KSP Waypoint, found again from the identifier that names it. The game builds
        /// its waypoints again whenever it loads a game state, so the waypoint this stands
        /// for is whichever one now carries the identifier.
        /// </summary>
        public FinePrint.Waypoint InternalWaypoint {
            get {
                var waypoint = Find ();
                if (waypoint == null)
                    throw NotResolvable ();
                return waypoint;
            }
        }

        /// <summary>
        /// What the game holds for the waypoint. It is live while the game's waypoint
        /// manager lists it, and destroyed once the manager is there to ask and does not, as
        /// a waypoint that leaves the list is gone for good. A game with no waypoint manager
        /// has no waypoints to look through, which says nothing about this one.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (FinePrint.WaypointManager.Instance () == null)
                    return GameObjectState.Dormant;
                return Find () != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        /// <summary>
        /// The waypoint the game currently has under the identifier, or null if it has none.
        /// </summary>
        FinePrint.Waypoint Find ()
        {
            var manager = FinePrint.WaypointManager.Instance ();
            if (manager == null)
                return null;
            var waypoints = manager.Waypoints;
            if (waypoints == null)
                return null;
            for (var i = 0; i < waypoints.Count; i++) {
                var waypoint = waypoints [i];
                if (waypoint != null && waypoint.navigationId == navigationId)
                    return waypoint;
            }
            return null;
        }

        /// <summary>
        /// The error to raise when no waypoint answers to the identifier, which
        /// <see cref="GameObjectState" /> decides between.
        /// </summary>
        Exception NotResolvable ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                return new ObjectDestroyedException (
                    "The waypoint no longer exists, as the game no longer has a waypoint with its id.");
            return new InvalidOperationException (
                "The waypoint is not loaded, as the game has no waypoint manager running. " +
                "It can be used again once the game does.");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Waypoint other)
        {
            return !ReferenceEquals (other, null) && navigationId == other.navigationId;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return navigationId.GetHashCode ();
        }

        /// <summary>
        /// The celestial body the waypoint is attached to.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body {
            get { return new CelestialBody (InternalWaypoint.celestialBody); }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set body for waypoint attached to a contract.");
                InternalWaypoint.celestialName = value.Name;
            }
        }

        /// <summary>
        /// The name of the waypoint as it appears on the map and the contract.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalWaypoint.FullName; }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set name for waypoint attached to a contract.");
                InternalWaypoint.name = value;
            }
        }

        /// <summary>
        /// The seed of the icon color. See <see cref="WaypointManager.Colors"/> for example colors.
        /// </summary>
        [KRPCProperty]
        public int Color {
            get { return InternalWaypoint.seed; }
            set { InternalWaypoint.seed = value; }
        }

        /// <summary>
        /// The icon of the waypoint.
        /// </summary>
        [KRPCProperty]
        public string Icon {
            get { return InternalWaypoint.id; }
            set { InternalWaypoint.id = value; }
        }

        /// <summary>
        /// The latitude of the waypoint.
        /// </summary>
        [KRPCProperty]
        public double Latitude {
            get { return InternalWaypoint.latitude; }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set latitude for waypoint attached to a contract.");
                InternalWaypoint.latitude = value;
            }
        }

        /// <summary>
        /// The longitude of the waypoint.
        /// </summary>
        [KRPCProperty]
        public double Longitude {
            get { return InternalWaypoint.longitude; }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set longitude for waypoint attached to a contract.");
                InternalWaypoint.longitude = value;
            }
        }

        /// <summary>
        /// The altitude of the waypoint above sea level, in meters.
        /// </summary>
        [KRPCProperty]
        public double MeanAltitude {
            get {
                return Body.BedrockHeight (Latitude, Longitude) + InternalWaypoint.altitude;
            }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set altitude for waypoint attached to a contract.");
                // KSP/FinePrint stores the waypoint altitude relative to the bedrock
                // (terrain) height. Convert from the mean (sea level) altitude so the
                // getter recovers it correctly and kRPC waypoints match KSP's convention.
                InternalWaypoint.altitude = value - Body.BedrockHeight (Latitude, Longitude);
                var surfaceAltitude = Body.SurfaceHeight (InternalWaypoint.latitude, InternalWaypoint.longitude);
                InternalWaypoint.isOnSurface = (Math.Abs (surfaceAltitude - value) < 10);
            }
        }

        /// <summary>
        /// The altitude of the waypoint above the surface of the body or sea level,
        /// whichever is closer, in meters.
        /// </summary>
        [KRPCProperty]
        public double SurfaceAltitude {
            get { return InternalWaypoint.altitude + Math.Min (0d, Body.BedrockHeight (Latitude, Longitude)); }
            set { MeanAltitude = value + Math.Max (0d, Body.BedrockHeight (Latitude, Longitude)); }
        }

        /// <summary>
        /// The altitude of the waypoint above the surface of the body, in meters.
        /// When over water, this is the altitude above the sea floor.
        /// </summary>
        [KRPCProperty]
        public double BedrockAltitude {
            get { return InternalWaypoint.altitude; }
            set { MeanAltitude = value + Body.BedrockHeight (Latitude, Longitude); }
        }

        /// <summary>
        /// <c>true</c> if the waypoint is near to the surface of a body.
        /// </summary>
        [KRPCProperty]
        public bool NearSurface {
            get { return InternalWaypoint.isOnSurface; }
        }

        /// <summary>
        /// <c>true</c> if the waypoint is attached to the ground.
        /// </summary>
        [KRPCProperty]
        public bool Grounded {
            get { return InternalWaypoint.landLocked; }
        }

        /// <summary>
        /// The integer index of this waypoint within its cluster of sibling waypoints.
        /// In other words, when you have a cluster of waypoints called "Somewhere Alpha",
        /// "Somewhere Beta" and "Somewhere Gamma", the alpha site has index 0, the beta
        /// site has index 1 and the gamma site has index 2.
        /// When <see cref="Clustered"/> is <c>false</c>, this is zero.
        /// </summary>
        [KRPCProperty]
        public int Index {
            get { return InternalWaypoint.index; }
        }

        /// <summary>
        /// <c>true</c> if this waypoint is part of a set of clustered waypoints with greek letter
        /// names appended (Alpha, Beta, Gamma, etc).
        /// If <c>true</c>, there is a one-to-one correspondence with the greek letter name and
        /// the <see cref="Index"/>.
        /// </summary>
        [KRPCProperty]
        public bool Clustered {
            get { return InternalWaypoint.isClustered; }
        }

        /// <summary>
        /// Whether the waypoint belongs to a contract.
        /// </summary>
        [KRPCProperty]
        public bool HasContract {
            get { return InternalWaypoint.contractReference != null; }
        }

        /// <summary>
        /// The associated contract.
        /// </summary>
        [KRPCProperty]
        public Contract Contract {
            get {
                if (!HasContract)
                    throw new InvalidOperationException("Waypoint does not have an associated contract");
                return new Contract(InternalWaypoint.contractReference);
            }
        }

        /// <summary>
        /// Removes the waypoint.
        /// </summary>
        /// <remarks>
        /// Any further use of this object throws an exception.
        /// </remarks>
        [KRPCMethod]
        public void Remove ()
        {
            var waypoint = InternalWaypoint;
            if (waypoint.contractReference != null)
                throw new InvalidOperationException ("Cannot remove waypoint attached to a contract.");
            FinePrint.WaypointManager.RemoveWaypoint (waypoint);
        }
    }
}
