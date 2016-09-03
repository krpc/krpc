using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using FinePrint;
using System.Linq;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents a waypoint. Can be created using <see cref="WaypointManager.AddWaypoint"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Waypoint : Equatable<Waypoint>
    {

        /// <summary>
        /// Create a waypoint object.
        /// </summary>
        internal Waypoint (double longitude, double latitude, CelestialBody body, string name)
        {
            InternalWaypoint = new FinePrint.Waypoint ();
            Name = name;
            Body = body;
            Icon = "report";
            Latitude = latitude;
            Color = 1115;
            Longitude = longitude;
            Altitude = body.InternalBody.TerrainAltitude (latitude, longitude);
            InternalWaypoint.isOnSurface = true;
            InternalWaypoint.isNavigatable = true;
            FinePrint.WaypointManager.AddWaypoint (InternalWaypoint);
        }

        /// <summary>
        /// Create a waypoint object from a KSP waypoint.
        /// </summary>
        /// <param name="wp"></param>
        public Waypoint (FinePrint.Waypoint wp)
        {
            this.InternalWaypoint = wp;
        }

        /// <summary>
        /// Celestial body the waypoint is attached to.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body
        {
            get { return new CelestialBody (InternalWaypoint.celestialBody); }
            set
            {
                if (HasContract) {
                    throw new System.Exception ("Cannot set body for waypoint attached to a contract.");
                } else {
                    InternalWaypoint.celestialName = value.Name;
                }
            }
        }

        /// <summary>
        /// Name of the waypoint as it appears on the map and the contract.
        /// </summary>
        [KRPCProperty]
        public string Name
        {
            get { return InternalWaypoint.FullName; }
            set
            {
                if (HasContract) {
                    throw new System.Exception ("Cannot set name for waypoint attached to a contract.");
                } else {
                    InternalWaypoint.name = value;
                }
            }
        }

        /// <summary>
        /// The seed of the icon color. See <see cref="WaypointManager.WaypointColors"/> for example colors.
        /// </summary>
        [KRPCProperty]
        public int Color
        {
            get { return InternalWaypoint.seed; }
            set { InternalWaypoint.seed = value; }
        }

        /// <summary>
        /// The icon of the waypoint.
        /// </summary>
        [KRPCProperty]
        public string Icon
        {
            get { return InternalWaypoint.id; }
            set { InternalWaypoint.id = value; }
        }

        /// <summary>
        /// The longitude of the waypoint.
        /// </summary>
        [KRPCProperty]
        public double Longitude
        {
            get { return InternalWaypoint.longitude; }
            set
            {
                if (HasContract) {
                    throw new System.Exception ("Cannot set longitude for waypoint attached to a contract.");
                } else {
                    InternalWaypoint.longitude = value;
                }
            }
        }

        /// <summary>
        /// The latitude of the waypoint.
        /// </summary>
        [KRPCProperty]
        public double Latitude
        {
            get { return InternalWaypoint.latitude; }
            set
            {
                if (HasContract) {
                    throw new System.Exception ("Cannot set latitude for waypoint attached to a contract.");
                } else {
                    InternalWaypoint.latitude = value;
                }
            }
        }

        /// <summary>
        /// Altitude of waypoint above ground level.
        /// </summary>
        [KRPCProperty]
        public double Altitude
        {
            get { return InternalWaypoint.altitude; }
            set
            {
                if (HasContract) {
                    throw new System.Exception ("Cannot set altitude for waypoint attached to a contract.");
                } else {
                    InternalWaypoint.altitude = value;
                }
            }
        }

        /// <summary>
        /// Altitude of waypoint above sea level.
        /// </summary>
        [KRPCProperty]
        public double AltitudeSeaLevel
        {
            get { return Body.SurfaceHeight (Latitude, Longitude) + Altitude; }
        }

        /// <summary>
        /// True if waypoint is a point near or on the body rather than high in orbit.
        /// </summary>
        [KRPCProperty]
        public bool NearSurface
        {
            get { return InternalWaypoint.isOnSurface; }
        }

        /// <summary>
        /// True if waypoint is actually glued to the ground.
        /// </summary>
        [KRPCProperty]
        public bool Grounded
        {
            get { return InternalWaypoint.landLocked; }
        }

        /// <summary>
        /// The integer index of this waypoint amongst its cluster of sibling waypoints. 
        /// In other words, when you have a cluster of waypoints called "Somewhere Alpha", "Somewhere Beta", and "Somewhere Gamma", 
        /// then the alpha site has index 0, the beta site has index 1 and the gamma site has index 2. 
        /// When Clustered is false, this value is zero but meaningless.
        /// </summary>
        [KRPCProperty]
        public int Index
        {
            get { return InternalWaypoint.index; }
        }

        /// <summary>
        /// True if this waypoint is part of a set of clustered waypoints with greek letter names appended (Alpha, Beta, Gamma, etc). 
        /// If true, there should be a one-to-one correspondence with the greek letter name and the :INDEX suffix. (0 = Alpha, 1 = Beta, 2 = Gamma, etc).
        /// </summary>
        [KRPCProperty]
        public bool Clustered
        {
            get { return InternalWaypoint.isClustered; }
        }

        /// <summary>
        /// Id of the associated contract. 
        /// Returns 0 if the waypoint does not belong to a contract.
        /// </summary>
        [KRPCProperty]
        public long ContractId
        {
            get { return HasContract ? InternalWaypoint.contractReference.ContractID : 0; }
        }

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

            int hash = InternalWaypoint.GetHashCode ();
            return hash;
        }


        /// <summary>
        /// The KSP Waypoint.
        /// </summary>
        public FinePrint.Waypoint InternalWaypoint
        { get; private set; }

        /// <summary>
        /// Whether the waypoint belongs to a contract.
        /// </summary> 
        public bool HasContract
        {
            get { return InternalWaypoint.contractReference != null; }
        }
    }
}
