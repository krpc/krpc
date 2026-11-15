using System;
using System.Collections;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.MechJeb
{
    /// <summary>
    /// A waypoint of the <see cref="RoverAutopilot"/>, as the Waypoints window shows it.
    /// </summary>
    /// <remarks>
    /// The object is destroyed once the waypoints are cleared or replaced, as by
    /// <see cref="RoverAutopilot.ClearWaypoints"/> and <see cref="RoverAutopilot.DriveToTarget"/>.
    /// </remarks>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RoverWaypoint : Equatable<RoverWaypoint>, IGameObjectState
    {
        static Field<object, double> latitude;
        static Field<object, double> longitude;
        static Field<object, float> radius;
        static Field<object, string> name;
        static Field<object, float> minSpeed;
        static Field<object, float> maxSpeed;
        static Func<object, global::Vessel> target;

        internal static void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebWaypoint");
            latitude = type.Field<object, double> ("Latitude");
            longitude = type.Field<object, double> ("Longitude");
            radius = type.Field<object, float> ("Radius");
            name = type.Field<object, string> ("Name");
            minSpeed = type.Field<object, float> ("MinSpeed");
            maxSpeed = type.Field<object, float> ("MaxSpeed");
            target = type.Getter<object, global::Vessel> ("Target");
        }

        // The rover controller's list of waypoints, which lasts as long as the controller
        readonly IList list;

        internal RoverWaypoint (IList waypoints, object instance)
        {
            list = waypoints;
            Instance = instance;
        }

        internal object Instance { get; private set; }

        // The waypoint, checked to be in the list
        object Live {
            get {
                if (!list.Contains (Instance))
                    throw new ObjectDestroyedException ("The waypoint was removed from the waypoints");
                return Instance;
            }
        }

        /// <summary>
        /// Live while the waypoint is in the rover controller's list. MechJeb builds new
        /// waypoint objects when it loads a list, so a waypoint out of the list is destroyed.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return list.Contains (Instance) ? GameObjectState.Live : GameObjectState.Destroyed; }
        }

        /// <summary>
        /// Whether the waypoints are the same MechJeb waypoint.
        /// </summary>
        public override bool Equals (RoverWaypoint other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (Instance, other.Instance);
        }

        /// <summary>
        /// Hash the waypoint.
        /// </summary>
        public override int GetHashCode ()
        {
            return RuntimeHelpers.GetHashCode (Instance);
        }

        // MechJeb moves a waypoint at a vessel to the vessel's position each physics frame
        void CheckPosition ()
        {
            if (target (Live) != null)
                throw new InvalidOperationException ("The waypoint is at a vessel");
        }

        /// <summary>
        /// The latitude of the waypoint, in degrees. Throws an exception when set if the waypoint
        /// is at a vessel.
        /// </summary>
        [KRPCProperty]
        public double Latitude {
            get { return latitude.Get (Live); }
            set {
                CheckPosition ();
                latitude.Set (Live, value);
            }
        }

        /// <summary>
        /// The longitude of the waypoint, in degrees. Throws an exception when set if the waypoint
        /// is at a vessel.
        /// </summary>
        [KRPCProperty]
        public double Longitude {
            get { return longitude.Get (Live); }
            set {
                CheckPosition ();
                longitude.Set (Live, value);
            }
        }

        /// <summary>
        /// The distance from the waypoint at which the rover has reached it, in meters.
        /// </summary>
        [KRPCProperty]
        public float Radius {
            get { return radius.Get (Live); }
            set { radius.Set (Live, value); }
        }

        /// <summary>
        /// The name of the waypoint.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return name.Get (Live); }
            set { name.Set (Live, value ?? string.Empty); }
        }

        /// <summary>
        /// The lowest speed on the way to the waypoint, in <unit>m/s</unit>, or 0 for no limit.
        /// </summary>
        [KRPCProperty]
        public float MinSpeed {
            get { return minSpeed.Get (Live); }
            set { minSpeed.Set (Live, value); }
        }

        /// <summary>
        /// The highest speed on the way to the waypoint, in <unit>m/s</unit>, or 0 for no limit.
        /// </summary>
        [KRPCProperty]
        public float MaxSpeed {
            get { return maxSpeed.Get (Live); }
            set { maxSpeed.Set (Live, value); }
        }
    }
}
