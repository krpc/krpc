using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a waypoint. Can be created using <see cref="WaypointManager.AddWaypoint"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Waypoint : Equatable<Waypoint>
    {
        /// <summary>
        ///
        /// Create a waypoint object.
        /// </summary>
        internal Waypoint (double latitude, double longitude, CelestialBody body, string name)
        {
            InternalWaypoint = new FinePrint.Waypoint ();
            Name = name;
            Body = body;
            Icon = "report";
            Latitude = latitude;
            Color = 1115;
            Longitude = longitude;
            MeanAltitude = Body.SurfaceHeight (latitude, longitude);
            InternalWaypoint.isOnSurface = true;
            InternalWaypoint.isNavigatable = true;
            FinePrint.WaypointManager.AddWaypoint (InternalWaypoint);
        }

        /// <summary>
        /// Create a waypoint object from a KSP waypoint.
        /// </summary>
        public Waypoint (FinePrint.Waypoint wp)
        {
            InternalWaypoint = wp;
        }

        /// <summary>
        /// The KSP Waypoint.
        /// </summary>
        public FinePrint.Waypoint InternalWaypoint { get; private set; }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Waypoint other)
        {
            return !ReferenceEquals (other, null) && InternalWaypoint == other.InternalWaypoint;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // Note: InternalNode could be set to null by Remove
            return InternalWaypoint.GetHashCode ();
        }

        /// <summary>
        /// Celestial body the waypoint is attached to.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body {
            get { return new CelestialBody (InternalWaypoint.celestialBody); }
            set {
                if (ReferenceEquals (value, null))
                    throw new ArgumentNullException ("Body");
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set body for waypoint attached to a contract.");
                InternalWaypoint.celestialName = value.Name;
            }
        }

        /// <summary>
        /// Name of the waypoint as it appears on the map and the contract.
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
            get { return InternalWaypoint.altitude; }
            set {
                if (HasContract)
                    throw new InvalidOperationException ("Cannot set altitude for waypoint attached to a contract.");
                InternalWaypoint.altitude = value;
            }
        }

        /// <summary>
        /// The altitude of the waypoint above the surface of the body or sea level, whichever is closer, in meters.
        /// </summary>
        [KRPCProperty]
        public double SurfaceAltitude {
            get { return InternalWaypoint.altitude - Math.Max (0d, Body.BedrockHeight (Latitude, Longitude)); }
            set { MeanAltitude = value + Math.Max (0d, Body.BedrockHeight (Latitude, Longitude)); }
        }

        /// <summary>
        /// The altitude of the waypoint above the surface of the body, in meters. When over water, this is the altitude above the sea floor.
        /// </summary>
        [KRPCProperty]
        public double BedrockAltitude {
            get { return InternalWaypoint.altitude - Body.BedrockHeight (Latitude, Longitude); }
            set { MeanAltitude = value + Body.BedrockHeight (Latitude, Longitude); }
        }

        /// <summary>
        /// True if waypoint is a point near or on the body rather than high in orbit.
        /// </summary>
        [KRPCProperty]
        public bool NearSurface {
            get { return InternalWaypoint.isOnSurface; }
        }

        /// <summary>
        /// True if waypoint is actually glued to the ground.
        /// </summary>
        [KRPCProperty]
        public bool Grounded {
            get { return InternalWaypoint.landLocked; }
        }

        /// <summary>
        /// The integer index of this waypoint amongst its cluster of sibling waypoints.
        /// In other words, when you have a cluster of waypoints called "Somewhere Alpha", "Somewhere Beta", and "Somewhere Gamma",
        /// then the alpha site has index 0, the beta site has index 1 and the gamma site has index 2.
        /// When <see cref="Clustered"/> is false, this value is zero but meaningless.
        /// </summary>
        [KRPCProperty]
        public int Index {
            get { return InternalWaypoint.index; }
        }

        /// <summary>
        /// True if this waypoint is part of a set of clustered waypoints with greek letter names appended (Alpha, Beta, Gamma, etc).
        /// If true, there is a one-to-one correspondence with the greek letter name and the <see cref="Index"/>.
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
        [KRPCMethod]
        public void Remove ()
        {
            if (HasContract)
                throw new InvalidOperationException ("Cannot remove waypoint attached to a contract.");
            FinePrint.WaypointManager.RemoveWaypoint (InternalWaypoint);
        }
    }
}
