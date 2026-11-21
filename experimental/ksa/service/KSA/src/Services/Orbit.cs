using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// An orbit. Obtained from <see cref="Vehicle.Orbit"/>, <see cref="Body.Orbit"/>,
    /// <see cref="Burn.Orbit"/> or <see cref="NextOrbit"/>.
    /// </summary>
    /// <remarks>
    /// An orbit follows the game's prediction as the trajectory changes. A predicted orbit, from
    /// <see cref="NextOrbit"/> or <see cref="Burn.Orbit"/>, is a position in the vehicle's or the
    /// burn's predicted trajectory, and throws an exception while the trajectory is too short to
    /// reach it.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Orbit : Equatable<Orbit>, IGameObjectState
    {
        // One of: a patch of the vehicle's predicted trajectory, where patch 0 is its current
        // orbit, a patch of the trajectory after a burn, or a body's orbit
        readonly global::KSA.Vehicle vehicle;
        readonly Burn burn;
        readonly int patchIndex;
        readonly global::KSA.Orbit fixedOrbit;

        internal Orbit (global::KSA.Vehicle vehicle, int patchIndex = 0)
        {
            this.vehicle = vehicle;
            this.patchIndex = patchIndex;
        }

        internal Orbit (Burn burn, int patchIndex = 0)
        {
            this.burn = burn;
            this.patchIndex = patchIndex;
        }

        internal Orbit (global::KSA.Orbit orbit)
        {
            fixedOrbit = orbit;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Orbit other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle) &&
                Equals (burn, other.burn) && patchIndex == other.patchIndex &&
                ReferenceEquals (fixedOrbit, other.fixedOrbit);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return State; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return HashCode.Combine (vehicle, burn, patchIndex, fixedOrbit);
        }

        // The vehicle replaces its orbit and flight plans as its trajectory changes, so we read
        // them on each call
        FlightPlan Plan {
            get { return burn != null ? burn.FlightPlan : new Vehicle (vehicle).InternalVehicle.FlightPlan; }
        }

        bool IsCurrent {
            get { return vehicle != null && patchIndex == 0; }
        }

        PatchedConic Patch {
            get {
                if (fixedOrbit != null)
                    return null;
                var patches = Plan.Patches;
                return patchIndex < patches.Count ? patches[patchIndex] : null;
            }
        }

        global::KSA.Orbit InternalOrbit {
            get {
                if (fixedOrbit != null)
                    return fixedOrbit;
                if (IsCurrent)
                    return new Vehicle (vehicle).InternalVehicle.Orbit;
                return Patch?.Orbit ?? throw new InvalidOperationException ("The trajectory does not reach the orbit");
            }
        }

        GameObjectState State {
            get {
                if (fixedOrbit != null)
                    return Vehicles.StateOf (fixedOrbit.Parent);
                if (burn != null)
                    return burn.GameObjectState;
                return Vehicles.StateOf (vehicle);
            }
        }

        static UniverseTime Now {
            get { return Universe.GetElapsedTime (); }
        }

        static double Radians (double degrees)
        {
            return degrees * (Math.PI / 180d);
        }

        /// <summary>
        /// The body being orbited.
        /// </summary>
        [KRPCProperty]
        public Body Body {
            get { return new Body (InternalOrbit.Parent); }
        }

        /// <summary>
        /// The apoapsis of the orbit, in meters, from the center of the body being orbited.
        /// Not a number for a parabolic or hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double Apoapsis {
            get { return InternalOrbit.Apoapsis; }
        }

        /// <summary>
        /// The periapsis of the orbit, in meters, from the center of the body being orbited.
        /// </summary>
        [KRPCProperty]
        public double Periapsis {
            get { return InternalOrbit.Periapsis; }
        }

        /// <summary>
        /// The apoapsis of the orbit, in meters, above the mean radius of the body being orbited.
        /// </summary>
        [KRPCProperty]
        public double ApoapsisAltitude {
            get { return InternalOrbit.ApoapsisAltitude; }
        }

        /// <summary>
        /// The periapsis of the orbit, in meters, above the mean radius of the body being orbited.
        /// </summary>
        [KRPCProperty]
        public double PeriapsisAltitude {
            get { return InternalOrbit.PeriapsisAltitude; }
        }

        /// <summary>
        /// The semi-major axis of the orbit, in meters. Negative for a hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double SemiMajorAxis {
            get { return InternalOrbit.SemiMajorAxis; }
        }

        /// <summary>
        /// The semi-minor axis of the orbit, in meters. Not a number for a parabolic or
        /// hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double SemiMinorAxis {
            get {
                var orbit = InternalOrbit;
                return orbit.Eccentricity < 1 ? orbit.SemiMinorAxis : double.NaN;
            }
        }

        /// <summary>
        /// The mean angular motion of the orbit, in radians per second.
        /// </summary>
        [KRPCProperty]
        public double MeanMotion {
            get { return InternalOrbit.GetMeanMotion (); }
        }

        /// <summary>
        /// The specific orbital energy, the sum of the kinetic and potential energy per unit
        /// mass, in joules per kilogram. Negative for an elliptical orbit.
        /// </summary>
        [KRPCProperty]
        public double SpecificEnergy {
            get {
                var orbit = InternalOrbit;
                return -orbit.Mu / (2 * orbit.SemiMajorAxis);
            }
        }

        /// <summary>
        /// The current orbital speed, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Speed {
            get { return InternalOrbit.GetVelocityCciAt (Now).Length (); }
        }

        /// <summary>
        /// The current distance from the center of the body being orbited, in meters.
        /// </summary>
        [KRPCProperty]
        public double Radius {
            get { return RadiusAt (Universe.GetElapsedSeconds ()); }
        }

        /// <summary>
        /// The orbital period, in seconds. Not a number for a parabolic or hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double Period {
            get { return InternalOrbit.Period; }
        }

        /// <summary>
        /// The time until the next apoapsis, in seconds. Not a number for a parabolic or
        /// hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double TimeToApoapsis {
            get {
                var orbit = InternalOrbit;
                if (orbit.Eccentricity >= 1)
                    return double.NaN;
                var time = orbit.GetNextApoapsisTime (Now);
                return time.HasValue ? (time.Value - Now).Seconds () : double.NaN;
            }
        }

        /// <summary>
        /// The time until the next periapsis, in seconds. Negative once a parabolic or
        /// hyperbolic orbit has passed it.
        /// </summary>
        [KRPCProperty]
        public double TimeToPeriapsis {
            get {
                var time = InternalOrbit.GetNextPeriapsisTime (Now);
                return time.HasValue ? (time.Value - Now).Seconds () : double.NaN;
            }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_eccentricity">eccentricity</a>
        /// of the orbit.
        /// </summary>
        [KRPCProperty]
        public double Eccentricity {
            get { return InternalOrbit.Eccentricity; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_inclination">inclination</a>
        /// of the orbit, in radians, against the equator of the body being orbited.
        /// </summary>
        [KRPCProperty]
        public double Inclination {
            get { return InternalOrbit.Inclination; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Longitude_of_the_ascending_node">longitude
        /// of the ascending node</a>, in radians.
        /// </summary>
        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return InternalOrbit.LongitudeOfAscendingNode; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Argument_of_periapsis">argument of
        /// periapsis</a>, in radians.
        /// </summary>
        [KRPCProperty]
        public double ArgumentOfPeriapsis {
            get { return InternalOrbit.ArgumentOfPeriapsis; }
        }

        /// <summary>
        /// The current <a href="https://en.wikipedia.org/wiki/True_anomaly">true anomaly</a>,
        /// in radians.
        /// </summary>
        [KRPCProperty]
        public double TrueAnomaly {
            get { return TrueAnomalyAtUT (Universe.GetElapsedSeconds ()); }
        }

        /// <summary>
        /// The current <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly</a>,
        /// in radians.
        /// </summary>
        [KRPCProperty]
        public double MeanAnomaly {
            get {
                var orbit = InternalOrbit;
                return Radians (orbit.GetMeanAnomaly (orbit.GetStateVectorsAt (Now).TrueAnomaly).Degrees);
            }
        }

        /// <summary>
        /// The current <a href="https://en.wikipedia.org/wiki/Eccentric_anomaly">eccentric
        /// anomaly</a>, in radians. For a hyperbolic orbit, this is the hyperbolic anomaly.
        /// </summary>
        [KRPCProperty]
        public double EccentricAnomaly {
            get {
                var orbit = InternalOrbit;
                return Radians (orbit.GetEccentricAnomaly (orbit.GetStateVectorsAt (Now).TrueAnomaly).Degrees);
            }
        }

        /// <summary>
        /// The true anomaly at the given time, in radians.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        [KRPCMethod]
        public double TrueAnomalyAtUT (double ut)
        {
            return Radians (InternalOrbit.GetStateVectorsAt (new UniverseTime (ut)).TrueAnomaly.Degrees);
        }

        /// <summary>
        /// The next universal time, from now, at which the orbit reaches the given true
        /// anomaly, in seconds. Not a number if it never does, such as beyond the asymptote of
        /// a hyperbolic orbit.
        /// </summary>
        /// <param name="trueAnomaly">True anomaly, in radians.</param>
        [KRPCMethod]
        public double UTAtTrueAnomaly (double trueAnomaly)
        {
            var time = InternalOrbit.TimeOfTrueAnomaly (new global::KSA.TrueAnomaly (trueAnomaly), Now, suppressWarnings: true);
            return time.HasValue ? time.Value.Seconds () : double.NaN;
        }

        /// <summary>
        /// The distance from the center of the body being orbited at the given time, in
        /// meters.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        [KRPCMethod]
        public double RadiusAt (double ut)
        {
            return InternalOrbit.GetStateVectorsAt (new UniverseTime (ut)).PositionCci.Length ();
        }

        /// <summary>
        /// The position on the orbit at the given time, in meters, relative to where the body
        /// being orbited is now.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        [KRPCMethod]
        public Tuple<double, double, double> PositionAt (double ut, ReferenceFrame referenceFrame)
        {
            var orbit = InternalOrbit;
            var positionCci = orbit.GetStateVectorsAt (new UniverseTime (ut)).PositionCci;
            var positionEcl = orbit.Parent.GetPositionEcl () + positionCci.Transform (orbit.Parent.GetCci2Cce ());
            return Vectors.ToTuple (referenceFrame.PositionFromEcl (positionEcl));
        }

        /// <summary>
        /// The orbital velocity at the given time, in meters per second, relative to the body
        /// being orbited and along the axes of the given frame.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        /// <param name="referenceFrame">The reference frame of the velocity.</param>
        [KRPCMethod]
        public Tuple<double, double, double> VelocityAt (double ut, ReferenceFrame referenceFrame)
        {
            var orbit = InternalOrbit;
            var velocityCci = orbit.GetVelocityCciAt (new UniverseTime (ut));
            return Vectors.ToTuple (referenceFrame.DirectionFromEcl (velocityCci.Transform (orbit.Parent.GetCci2Cce ())));
        }

        /// <summary>
        /// The inclination of the orbit relative to another orbit, in radians.
        /// </summary>
        /// <param name="orbit">The other orbit.</param>
        [KRPCMethod]
        public double RelativeInclination (Orbit orbit)
        {
            if (ReferenceEquals (orbit, null))
                throw new ArgumentNullException (nameof (orbit));
            return InternalOrbit.GetRelativeInclination (orbit.InternalOrbit).Value ();
        }

        /// <summary>
        /// The true anomaly of the ascending node with another orbit, in radians.
        /// </summary>
        /// <param name="orbit">The other orbit.</param>
        /// <remarks>
        /// Throws an exception if the orbits are around different bodies.
        /// </remarks>
        [KRPCMethod]
        public double TrueAnomalyAtAN (Orbit orbit)
        {
            var other = OtherOrbit (orbit);
            return Radians (InternalOrbit.GetAscendingNode (other).Degrees);
        }

        /// <summary>
        /// The true anomaly of the descending node with another orbit, in radians.
        /// </summary>
        /// <param name="orbit">The other orbit.</param>
        /// <remarks>
        /// Throws an exception if the orbits are around different bodies.
        /// </remarks>
        [KRPCMethod]
        public double TrueAnomalyAtDN (Orbit orbit)
        {
            var other = OtherOrbit (orbit);
            return Radians (InternalOrbit.GetDescendingNode (other).Degrees);
        }

        global::KSA.Orbit OtherOrbit (Orbit orbit)
        {
            if (ReferenceEquals (orbit, null))
                throw new ArgumentNullException (nameof (orbit));
            var other = orbit.InternalOrbit;
            if (!ReferenceEquals (other.Parent, InternalOrbit.Parent))
                throw new ArgumentException ("The orbits are around different bodies");
            return other;
        }

        // The closest approach on this orbit to the target of the vehicle it belongs to. The
        // game finds them while it predicts the vehicle's trajectory
        Encounter? ClosestApproach {
            get {
                var patch = Patch;
                var target = IsCurrent ? new Vehicle (vehicle).InternalVehicle.Target : null;
                if (patch == null || target == null)
                    return null;
                return patch.TryFindClosestApproach (target);
            }
        }

        /// <summary>
        /// The universal time of the closest approach on this orbit to the vehicle's target,
        /// in seconds. Not a number if there is none.
        /// </summary>
        /// <remarks>
        /// Only the vehicle's current orbit has a target. The game predicts the trajectory on a
        /// worker thread, so a new target shows here shortly after it is set.
        /// </remarks>
        [KRPCProperty]
        public double TimeOfClosestApproach {
            get { return ClosestApproach?.GameTime.Seconds () ?? double.NaN; }
        }

        /// <summary>
        /// The distance at the closest approach on this orbit to the vehicle's target, in
        /// meters. Not a number if there is none.
        /// </summary>
        /// <remarks>
        /// Only the vehicle's current orbit has a target. The game predicts the trajectory on a
        /// worker thread, so a new target shows here shortly after it is set.
        /// </remarks>
        [KRPCProperty]
        public double DistanceAtClosestApproach {
            get { return ClosestApproach?.ClosestDistance ?? double.NaN; }
        }

        /// <summary>
        /// The universal time at which this orbit starts, in seconds. Not a number for the
        /// orbit of a body.
        /// </summary>
        [KRPCProperty]
        public double StartUT {
            get {
                var patch = Patch;
                return patch == null ? double.NaN : patch.StartTime.Seconds ();
            }
        }

        /// <summary>
        /// The universal time at which this orbit ends, in seconds. Infinite for an orbit that
        /// does not end, such as the orbit of a body.
        /// </summary>
        /// <remarks>
        /// <see cref="EndTransition"/> gives how the orbit ends.
        /// </remarks>
        [KRPCProperty]
        public double EndUT {
            get {
                var patch = Patch;
                if (patch == null || patch.EndTransition == PatchTransition.Final || patch.EndTime >= UniverseTime.EndOfTime)
                    return double.PositiveInfinity;
                return patch.EndTime.Seconds ();
            }
        }

        /// <summary>
        /// How the orbit ends.
        /// </summary>
        [KRPCProperty]
        public OrbitTransition EndTransition {
            get {
                var patch = Patch;
                if (patch == null)
                    return OrbitTransition.None;
                switch (patch.EndTransition) {
                case PatchTransition.Encounter:
                    return OrbitTransition.Encounter;
                case PatchTransition.Escape:
                    return OrbitTransition.Escape;
                case PatchTransition.Impact:
                    return OrbitTransition.Impact;
                case PatchTransition.Burn:
                    return OrbitTransition.Burn;
                default:
                    return OrbitTransition.None;
                }
            }
        }

        /// <summary>
        /// The time until the orbit enters or leaves a sphere of influence, in seconds. Not a
        /// number if it does neither.
        /// </summary>
        [KRPCProperty]
        public double TimeToSOIChange {
            get {
                var transition = EndTransition;
                if (transition != OrbitTransition.Encounter && transition != OrbitTransition.Escape)
                    return double.NaN;
                return EndUT - Universe.GetElapsedSeconds ();
            }
        }

        /// <summary>
        /// The orbit that follows this one after it enters or leaves a sphere of influence, or
        /// <c>null</c> if there is none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Orbit NextOrbit {
            get {
                var transition = EndTransition;
                if (transition != OrbitTransition.Encounter && transition != OrbitTransition.Escape)
                    return null;
                if (patchIndex + 1 >= Plan.Patches.Count)
                    return null;
                return burn != null ? new Orbit (burn, patchIndex + 1) : new Orbit (vehicle, patchIndex + 1);
            }
        }
    }
}
