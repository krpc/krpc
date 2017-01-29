using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Waypoints are the location markers you can see on the map view showing you where contracts are targeted for.
    /// With this structure, you can obtain coordinate data for the locations of these waypoints.
    /// Obtained by calling <see cref="SpaceCenter.WaypointManager"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class WaypointManager : Equatable<WaypointManager>
    {
        IList<string> icons;
        IDictionary<string, int> colors;

        internal WaypointManager ()
        {
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (WaypointManager other)
        {
            return !ReferenceEquals (other, null);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return 0;
        }

        /// <summary>
        /// A list of all existing waypoints.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public IList<Waypoint> Waypoints {
            get {
                var wpm = FinePrint.WaypointManager.Instance ();
                if (wpm == null)
                    return new List<Waypoint> ();
                return wpm.Waypoints.Select (wp => new Waypoint (wp)).ToList ();
            }
        }

        /// <summary>
        /// Creates a waypoint at the given position at ground level, and returns a
        /// <see cref="Waypoint"/> object that can be used to modify it.
        /// </summary>
        /// <param name="latitude">Latitude of the waypoint.</param>
        /// <param name="longitude">Longitude of the waypoint.</param>
        /// <param name="body">Celestial body the waypoint is attached to.</param>
        /// <param name="name">Name of the waypoint.</param>
        /// <returns></returns>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Correctness", "MethodCanBeMadeStaticRule")]
        public Waypoint AddWaypoint (double latitude, double longitude, CelestialBody body, string name)
        {
            return new Waypoint (latitude, longitude, body, name);
        }

        /// <summary>
        /// Returns all available icons (from "GameData/Squad/Contracts/Icons/").
        /// </summary>
        [KRPCProperty]
        public IList<string> Icons {
            get {
                if (icons == null) {
                    icons = GameDatabase.Instance.databaseTexture
                        .Where (t => t.name.StartsWith ("Squad/Contracts/Icons/", StringComparison.CurrentCulture))
                        .Select (texInfo => texInfo.name.Replace ("Squad/Contracts/Icons/", string.Empty))
                        .ToList ();
                }
                return icons;
            }
        }

        /// <summary>
        /// An example map of known color - seed pairs.
        /// Any other integers may be used as seed.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, int> Colors {
            get {
                if (colors == null) {
                    colors = new Dictionary<string, int> {
                        { "blue", 1115 },
                        { "green", 487 },
                        { "lightblue", 651 },
                        { "orange", 16 },
                        { "purple", 665 },
                        { "red", 316 },
                        { "yellow", 23 }
                    };
                }
                return colors;
            }
        }
    }
}
