using System;
using System.Collections;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Rover Autopilot window, which drives a rover at a heading and speed, or along its
    /// waypoints.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class RoverAutopilot : ComputerModule
    {
        // The radius at which a waypoint on a vessel counts as reached, as in MechJeb's window
        const float vesselWaypointRadius = 25f;
        const float defaultWaypointRadius = 5f;

        static Field<object, bool> controlHeading;
        static Field<object, double> heading;
        static Func<object, double> headingErr;
        static Field<object, bool> controlSpeed;
        static Field<object, double> speed;
        static Func<object, double> speedErr;
        static Field<object, bool> stabilityControl;
        static Field<object, bool> brakeOnEject;
        static Field<object, bool> brakeOnEnergyDepletion;
        static Field<object, bool> warpToDaylight;
        static Func<object, double> tgtSpeed;
        static Func<object, IList> waypoints;
        static Field<object, int> waypointIndex;
        static Field<object, bool> loopWaypoints;
        static Func<double, double, float, string, float, float, object> newWaypoint;
        static Func<global::Vessel, float, string, float, float, object> newVesselWaypoint;
        static Action<object> updateWindowUsers;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleRoverController");
            controlHeading = type.Field<object, bool> ("ControlHeading");
            heading = Bindings.EditableDouble (type, "heading");
            headingErr = type.Getter<object, double> ("headingErr");
            controlSpeed = type.Field<object, bool> ("ControlSpeed");
            speed = Bindings.EditableDouble (type, "speed");
            speedErr = type.Getter<object, double> ("speedErr");
            // MechJeb declares these read-only, and its GUI toggles write them by reflection
            stabilityControl = type.ReadOnlyField<object, bool> ("StabilityControl");
            brakeOnEject = type.ReadOnlyField<object, bool> ("BrakeOnEject");
            brakeOnEnergyDepletion = type.ReadOnlyField<object, bool> ("BrakeOnEnergyDepletion");
            warpToDaylight = type.ReadOnlyField<object, bool> ("WarpToDaylight");
            tgtSpeed = type.Getter<object, double> ("tgtSpeed");
            waypoints = type.Getter<object, IList> ("Waypoints");
            waypointIndex = type.Field<object, int> ("WaypointIndex");
            loopWaypoints = type.Field<object, bool> ("LoopWaypoints");
            var waypoint = Bindings.Type ("MuMech.MechJebWaypoint");
            newWaypoint = waypoint.Constructor<Func<double, double, float, string, float, float, object>> ();
            newVesselWaypoint = waypoint.Constructor<Func<global::Vessel, float, string, float, float, object>> ();
            updateWindowUsers = Bindings.Type ("MuMech.MechJebModuleRoverWindow").Method<Action<object>> ("OnUpdate");
            RoverWaypoint.Load ();
        }

        internal RoverAutopilot () : base ("MechJebModuleRoverController")
        {
        }

        /// <summary>
        /// Enable the controller while any of its controls is on. MechJeb's rover window holds
        /// the controller for them, and the controller runs this each frame while the window is
        /// closed, so it lets go once a drive ends.
        /// </summary>
        static void UpdateUsers ()
        {
            updateWindowUsers (MechJeb.Module ("MechJebModuleRoverWindow"));
        }

        /// <summary>
        /// Whether the autopilot holds <see cref="Heading"/>.
        /// </summary>
        [KRPCProperty]
        public bool ControlHeading {
            get { return controlHeading.Get (Instance); }
            set {
                controlHeading.Set (Instance, value);
                UpdateUsers ();
            }
        }

        /// <summary>
        /// The heading to hold, in degrees.
        /// </summary>
        /// <remarks>Requires <see cref="ControlHeading"/>.</remarks>
        [KRPCProperty]
        public double Heading {
            get { return heading.Get (Instance); }
            set { heading.Set (Instance, value); }
        }

        /// <summary>
        /// The difference between the vessel's heading and the heading to hold, in degrees.
        /// </summary>
        [KRPCProperty]
        public double HeadingError {
            get { return headingErr (Instance); }
        }

        /// <summary>
        /// Whether the autopilot holds <see cref="Speed"/>.
        /// </summary>
        [KRPCProperty]
        public bool ControlSpeed {
            get { return controlSpeed.Get (Instance); }
            set {
                controlSpeed.Set (Instance, value);
                UpdateUsers ();
            }
        }

        /// <summary>
        /// The speed to hold, in <unit>m/s</unit>.
        /// </summary>
        /// <remarks>Requires <see cref="ControlSpeed"/>.</remarks>
        [KRPCProperty]
        public double Speed {
            get { return speed.Get (Instance); }
            set { speed.Set (Instance, value); }
        }

        /// <summary>
        /// The difference between the vessel's speed and the speed to hold, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double SpeedError {
            get { return speedErr (Instance); }
        }

        /// <summary>
        /// The speed the autopilot drives at now, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double TargetSpeed {
            get { return tgtSpeed (Instance); }
        }

        /// <summary>
        /// Whether the autopilot keeps the rover upright.
        /// </summary>
        [KRPCProperty]
        public bool StabilityControl {
            get { return stabilityControl.Get (Instance); }
            set {
                stabilityControl.Set (Instance, value);
                UpdateUsers ();
            }
        }

        /// <summary>
        /// Whether to brake when the crew leaves the rover.
        /// </summary>
        [KRPCProperty]
        public bool BrakeOnEject {
            get { return brakeOnEject.Get (Instance); }
            set {
                brakeOnEject.Set (Instance, value);
                UpdateUsers ();
            }
        }

        /// <summary>
        /// Whether to brake when the rover runs out of electric charge.
        /// </summary>
        [KRPCProperty]
        public bool BrakeOnEnergyDepletion {
            get { return brakeOnEnergyDepletion.Get (Instance); }
            set {
                brakeOnEnergyDepletion.Set (Instance, value);
                UpdateUsers ();
            }
        }

        /// <summary>
        /// Whether to warp time to daylight after braking for a lack of electric charge.
        /// </summary>
        /// <remarks>Requires <see cref="BrakeOnEnergyDepletion"/>.</remarks>
        [KRPCProperty]
        public bool WarpToDaylight {
            get { return warpToDaylight.Get (Instance); }
            set { warpToDaylight.Set (Instance, value); }
        }

        /// <summary>
        /// The waypoints, in the order the rover drives to them.
        /// </summary>
        [KRPCProperty]
        public IList<RoverWaypoint> Waypoints {
            get {
                var list = waypoints (Instance);
                var result = new List<RoverWaypoint> ();
                foreach (var waypoint in list)
                    result.Add (new RoverWaypoint (list, waypoint));
                return result;
            }
        }

        /// <summary>
        /// The index of the waypoint the autopilot drives to, or -1 if there is none.
        /// </summary>
        [KRPCProperty]
        public int WaypointIndex {
            get { return waypointIndex.Get (Instance); }
        }

        /// <summary>
        /// Whether to return to the first waypoint after the last one.
        /// </summary>
        [KRPCProperty]
        public bool LoopWaypoints {
            get { return loopWaypoints.Get (Instance); }
            set { loopWaypoints.Set (Instance, value); }
        }

        /// <summary>
        /// Add a waypoint at a position on the surface of the body the vessel is at.
        /// </summary>
        /// <param name="latitude">The latitude, in degrees.</param>
        /// <param name="longitude">The longitude, in degrees.</param>
        /// <returns>The waypoint.</returns>
        [KRPCMethod]
        public RoverWaypoint AddWaypoint (double latitude, double longitude)
        {
            var waypoint = newWaypoint (latitude, longitude, defaultWaypointRadius, string.Empty, 0f, 0f);
            var list = waypoints (Instance);
            list.Add (waypoint);
            return new RoverWaypoint (list, waypoint);
        }

        /// <summary>
        /// Remove all the waypoints.
        /// </summary>
        [KRPCMethod]
        public void ClearWaypoints ()
        {
            waypointIndex.Set (Instance, -1);
            waypoints (Instance).Clear ();
        }

        /// <summary>
        /// Add a waypoint at the target. Throws an exception if the target is not a vessel or a
        /// position on the surface.
        /// </summary>
        /// <returns>The waypoint.</returns>
        [KRPCMethod]
        public RoverWaypoint AddTarget ()
        {
            var waypoint = TargetWaypoint ();
            var list = waypoints (Instance);
            list.Add (waypoint);
            return new RoverWaypoint (list, waypoint);
        }

        /// <summary>
        /// Replace the waypoints with the target, and drive to it. Throws an exception if the
        /// target is not a vessel or a position on the surface.
        /// </summary>
        [KRPCMethod]
        public void DriveToTarget ()
        {
            var waypoint = TargetWaypoint ();
            var instance = Instance;
            waypointIndex.Set (instance, 0);
            var list = waypoints (instance);
            list.Clear ();
            list.Add (waypoint);
            loopWaypoints.Set (instance, false);
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.Brakes, false);
            Drive ();
        }

        object TargetWaypoint ()
        {
            var target = MechJeb.TargetController;
            target.Update ();
            var targetable = target.Target;
            if (targetable == null)
                throw new InvalidOperationException ("There is no target");
            var vessel = targetable.GetVessel ();
            if (vessel != null)
                return newVesselWaypoint (vessel, vesselWaypointRadius, string.Empty, 0f, 0f);
            if (!target.PositionTargetExists)
                throw new InvalidOperationException ("The target is not a vessel or a position on the surface");
            return newWaypoint (target.TargetLatitude, target.TargetLongitude, defaultWaypointRadius, string.Empty, 0f, 0f);
        }

        /// <summary>
        /// Drive along the waypoints. Throws an exception if there are none.
        /// </summary>
        [KRPCMethod]
        public void Drive ()
        {
            var instance = Instance;
            if (waypoints (instance).Count == 0)
                throw new InvalidOperationException ("There are no waypoints");
            if (waypointIndex.Get (instance) < 0)
                waypointIndex.Set (instance, 0);
            controlHeading.Set (instance, true);
            controlSpeed.Set (instance, true);
            UpdateUsers ();
        }

        /// <summary>
        /// Stop driving along the waypoints.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            var instance = Instance;
            controlHeading.Set (instance, false);
            controlSpeed.Set (instance, false);
            loopWaypoints.Set (instance, false);
            UpdateUsers ();
        }
    }
}
