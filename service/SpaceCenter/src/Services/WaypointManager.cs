using KRPC.Service.Attributes;
using KRPC.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FinePrint.Utilities;

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

        internal WaypointManager ()
        {
        }

        /// <summary>
        /// Returns a list of all existing waypoints.
        /// </summary>
        [KRPCProperty]
        public IList<Waypoint> Waypoints
        {
            get
            {
                var wpm = FinePrint.WaypointManager.Instance ();
                if (wpm == null) {
                    return new List<Waypoint> ();
                }
                return wpm.Waypoints.Select (wp => new Waypoint (wp)).ToList ();
            }
        }

        /// <summary>
        /// Creates a waypoint at the given position at ground level, and returns a
        /// <see cref="Waypoint"/> object that can be used to modify it.
        /// </summary>
        /// <param name="longitude">Longitude of the waypoint.</param>
        /// <param name="latitude">Latitude of the waypoint.</param>
        /// <param name="name">Name of the waypoint.</param>
        /// <param name="body">Celestial body the waypoint is attached to.</param>
        /// <returns></returns>
        [KRPCMethod]
        public Waypoint AddWaypoint (double longitude, double latitude, CelestialBody body, string name)
        {
            return new Waypoint (longitude, latitude, body, name);
        }

        /// <summary>
        /// Removes the given waypoint.
        /// </summary>
        /// <param name="waypoint">The waypoint to remove</param>
        /// <returns></returns>
        [KRPCMethod]
        public void RemoveWaypoint (Waypoint waypoint)
        {
            if (waypoint.HasContract) {
                throw new System.Exception ("Cannot remove waypoint attached to a contract.");
            } else {
                FinePrint.WaypointManager.RemoveWaypoint (waypoint.InternalWaypoint);
            }
        }

        private IList<string> _icons;

        /// <summary>
        /// Returns all available icons (from "GameData/Squad/Contracts/Icons/").
        /// </summary>
        [KRPCProperty]
        public IList<string> Icons
        {
            get
            {
                if (_icons == null) {
                    var icons = new List<string> ();

                    foreach (GameDatabase.TextureInfo texInfo in GameDatabase.Instance.databaseTexture.Where (t => t.name.StartsWith ("Squad/Contracts/Icons/"))) {
                        string name = texInfo.name.Replace ("Squad/Contracts/Icons/", "");
                        icons.Add (name);
                    }
                    _icons = icons;
                }
                return _icons;
            }
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

        private IDictionary<string, int> _waypointColors;

        /// <summary>
        /// An example map of known color - seed pairs. 
        /// Any other integers may be used as seed.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string, int> WaypointColors
        {
            get
            {
                if (_waypointColors == null) {
                    _waypointColors = new Dictionary<string, int> ()
                    {
                        { "blue", 1115 },
                        { "green", 487 },
                        { "lightblue", 651 },
                        { "orange", 16 },
                        { "purple", 665 },
                        { "red", 316 },
                        { "yellow", 23 }
                    };
                }
                return _waypointColors;
            }
        }
    }
}
