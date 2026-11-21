using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The mission planned for a vehicle in the editor's life support window. Obtained from
    /// <see cref="Vehicle.Mission"/>.
    /// </summary>
    /// <remarks>
    /// The game times the legs of the mission from the plan, and checks the supplies of each
    /// habitat zone against them. See <see cref="HabitatZone.MissionSupplies"/>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Mission : Equatable<Mission>, IGameObjectState
    {
        readonly Vehicle vehicle;

        internal Mission (Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Mission other)
        {
            return !ReferenceEquals (other, null) && vehicle.Equals (other.vehicle);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = vehicle.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return vehicle.InternalVehicle.Parts.Mission == null ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        MissionPlan Plan {
            get {
                return vehicle.InternalVehicle.Parts.Mission ??
                    throw new Service.KRPC.ObjectDestroyedException ("The vehicle no longer has a mission");
            }
        }

        // The legs as planned
        internal MissionLegs PlannedLegs {
            get { return MissionPicker.ComputeLegs (Plan); }
        }

        // The legs left, counted down from the vehicle's launch, as the life support window's
        // "as planned" timing does
        internal MissionLegs RemainingLegs {
            get {
                var v = vehicle.InternalVehicle;
                return MissionProgress.FromLaunch (PlannedLegs, Universe.GetElapsedTime () - v.LaunchGameTime);
            }
        }

        /// <summary>
        /// Where the mission goes.
        /// </summary>
        [KRPCProperty]
        public MissionDestination Destination {
            get { return (MissionDestination)Plan.Destination; }
        }

        /// <summary>
        /// The body the mission goes to, or <c>null</c> if it has no destination. For a low
        /// orbit, this is the home body.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Body Body {
            get {
                var plan = Plan;
                var system = Universe.CurrentSystem;
                switch (plan.Destination) {
                case global::KSA.MissionDestination.LowOrbit:
                    return system?.HomeBody == null ? null : new Body (system.HomeBody);
                case global::KSA.MissionDestination.Body:
                    if (system != null && system.All.TryGet (plan.BodyId, out Astronomical body) && body is IParentBody parent)
                        return new Body (parent);
                    return null;
                default:
                    return null;
                }
            }
        }

        /// <summary>
        /// What the mission does at its destination.
        /// </summary>
        [KRPCProperty]
        public MissionArrival Arrival {
            get { return (MissionArrival)Plan.Arrival; }
        }

        /// <summary>
        /// Whether the mission comes back.
        /// </summary>
        [KRPCProperty]
        public MissionTrip Trip {
            get { return (MissionTrip)Plan.Trip; }
        }

        /// <summary>
        /// Whether the mission has a length set by the player, in place of legs timed from
        /// its destination. See <see cref="CustomLength"/>.
        /// </summary>
        [KRPCProperty]
        public bool Custom {
            get { return Plan.Custom; }
        }

        /// <summary>
        /// The length of a mission with <see cref="Custom"/> set, in seconds.
        /// </summary>
        [KRPCProperty]
        public float CustomLength {
            get { return (float)Plan.CustomLength.Seconds (); }
        }

        /// <summary>
        /// The time planned at the destination, in seconds.
        /// </summary>
        /// <remarks>
        /// The game rounds a stay before a return up to the next transfer window home. See
        /// <see cref="StayTime"/>.
        /// </remarks>
        [KRPCProperty]
        public float Stay {
            get { return (float)Plan.Stay.Seconds (); }
        }

        /// <summary>
        /// Whether the game can time the legs of the mission. A mission with no destination,
        /// or a flyby with no way back, cannot be timed.
        /// </summary>
        /// <remarks>
        /// The leg times are zero when this is false.
        /// </remarks>
        [KRPCProperty]
        public bool Feasible {
            get { return PlannedLegs.Status == MissionLegsStatus.Planned; }
        }

        /// <summary>
        /// The planned time of the way out, in seconds.
        /// </summary>
        [KRPCProperty]
        public float OutTime {
            get { return (float)PlannedLegs.Out.Seconds (); }
        }

        /// <summary>
        /// The planned time at the destination, in seconds.
        /// </summary>
        [KRPCProperty]
        public float StayTime {
            get { return (float)PlannedLegs.Stay.Seconds (); }
        }

        /// <summary>
        /// The planned time of the way back, in seconds. Zero for a one way trip.
        /// </summary>
        [KRPCProperty]
        public float BackTime {
            get { return (float)PlannedLegs.Back.Seconds (); }
        }

        /// <summary>
        /// The time from arriving at the destination until the next transfer window home, in
        /// seconds. Zero when there is no wait.
        /// </summary>
        [KRPCProperty]
        public float ReturnWait {
            get { return (float)PlannedLegs.ReturnWait.Seconds (); }
        }

        /// <summary>
        /// The planned length of the mission, in seconds.
        /// </summary>
        [KRPCProperty]
        public float TotalTime {
            get { return (float)PlannedLegs.Total.Seconds (); }
        }

        /// <summary>
        /// The time left in the mission as planned, counted from the vehicle's launch, in
        /// seconds.
        /// </summary>
        [KRPCProperty]
        public float TimeLeft {
            get { return (float)RemainingLegs.Total.Seconds (); }
        }

        /// <summary>
        /// The universal time the vehicle arrived at the destination body, in seconds. Not a
        /// number until it has.
        /// </summary>
        /// <remarks>
        /// The game records the arrival for a mission to another body.
        /// </remarks>
        [KRPCProperty]
        public double ArrivalUT {
            get { return Plan.ArrivedAt?.Seconds () ?? double.NaN; }
        }

        /// <summary>
        /// The universal time the vehicle left the destination body, in seconds. Not a number
        /// until it has.
        /// </summary>
        [KRPCProperty]
        public double DepartureUT {
            get { return Plan.LeftAt?.Seconds () ?? double.NaN; }
        }
    }
}
