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
        // Whether a client created the orbit, and whether it has since been removed
        bool created;
        bool removed;

        static readonly ClientObjects<Orbit> createdOrbits = new ClientObjects<Orbit> (x => x.removed = true);

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
                if (removed)
                    return GameObjectState.Destroyed;
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
        /// Create the orbit with the given elements around a body. The angles are those of
        /// <see cref="Orbit"/>.
        /// </summary>
        /// <param name="body">The body being orbited.</param>
        /// <param name="semiMajorAxis">The semi-major axis, in meters. Negative or positive
        /// for a hyperbolic orbit.</param>
        /// <param name="eccentricity">The eccentricity. Below 1 for an elliptical orbit, and
        /// above 1 for a hyperbolic one.</param>
        /// <param name="inclination">The inclination, in radians.</param>
        /// <param name="longitudeOfAscendingNode">The longitude of the ascending node, in radians.</param>
        /// <param name="argumentOfPeriapsis">The argument of periapsis, in radians.</param>
        /// <param name="meanAnomalyAtEpoch">The mean anomaly at the epoch, in radians.</param>
        /// <param name="epoch">The universal time of the epoch, in seconds.</param>
        /// <remarks>
        /// The orbit is a single conic around <paramref name="body"/>, with no
        /// <see cref="NextOrbit"/>. It is held until <see cref="Remove"/> is called on it, or
        /// the client that created it disconnects.
        /// </remarks>
        [KRPCMethod]
        public static Orbit CreateFromElements (
            Body body, double semiMajorAxis, double eccentricity, double inclination,
            double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            return Created (FromElements (
                body.InternalBody, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode,
                argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, default));
        }

        /// <summary>
        /// Create the orbit around a body through the given position and velocity.
        /// </summary>
        /// <param name="body">The body being orbited.</param>
        /// <param name="position">The position, in meters, relative to where the body is
        /// now.</param>
        /// <param name="velocity">The velocity, in meters per second.</param>
        /// <param name="ut">The universal time of the position and velocity, in seconds.</param>
        /// <param name="referenceFrame">The reference frame of the position and velocity.</param>
        /// <remarks>
        /// The orbit is a single conic around <paramref name="body"/>, with no
        /// <see cref="NextOrbit"/>. It is held until <see cref="Remove"/> is called on it, or
        /// the client that created it disconnects.
        /// </remarks>
        [KRPCMethod]
        public static Orbit CreateFromPositionAndVelocity (
            Body body, Tuple<double, double, double> position, Tuple<double, double, double> velocity,
            double ut, ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            if (!double.IsFinite (ut))
                throw new ArgumentException ("The time is not a finite number");
            var parent = body.InternalBody;
            var positionEcl = referenceFrame.PositionToEcl (Vectors.ToVector (position));
            var velocityEcl = referenceFrame.VelocityToEcl (positionEcl, Vectors.ToVector (velocity));
            var cce2Cci = parent.GetCci2Cce ().Inverse ();
            var positionCci = (positionEcl - parent.GetPositionEcl ()).Transform (cce2Cci);
            var velocityCci = (velocityEcl - parent.GetVelocityEcl ()).Transform (cce2Cci);
            if (positionCci.LengthSquared () < 1)
                throw new ArgumentException ("The position is at the center of the body");
            var orbit = global::KSA.Orbit.CreateFromStateCci (parent, new UniverseTime (ut), positionCci, velocityCci, default);
            if (!double.IsFinite (orbit.SemiMajorAxis) || !double.IsFinite (orbit.Eccentricity))
                throw new ArgumentException ("The position and velocity do not describe an orbit");
            return Created (orbit);
        }

        // The game's orbit with the given elements. The game's elements give the time at
        // periapsis, which we find from the mean motion.
        internal static global::KSA.Orbit FromElements (
            IParentBody parent, double semiMajorAxis, double eccentricity, double inclination,
            double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch,
            byte4 lineColor)
        {
            if (!(double.IsFinite (semiMajorAxis) && double.IsFinite (eccentricity) && double.IsFinite (inclination) &&
                  double.IsFinite (longitudeOfAscendingNode) && double.IsFinite (argumentOfPeriapsis) &&
                  double.IsFinite (meanAnomalyAtEpoch) && double.IsFinite (epoch)))
                throw new ArgumentException ("The orbital elements are not finite numbers");
            if (!(eccentricity >= 0 && eccentricity < 1 && semiMajorAxis > 0) &&
                !(eccentricity > 1 && semiMajorAxis != 0))
                throw new ArgumentException ("The orbit is not elliptical or hyperbolic");
            var periapsis = Math.Abs (semiMajorAxis * (1 - eccentricity));
            global::KSA.Orbit Create (UniverseTime timeAtPeriapsis) => global::KSA.Orbit.Create (parent, new OrbitData (
                timeAtPeriapsis, parent.Mass, periapsis, eccentricity, inclination, longitudeOfAscendingNode,
                argumentOfPeriapsis), lineColor);
            var now = Universe.GetElapsedTime ();
            var meanMotion = Create (now).GetMeanMotion ();
            return Create (now + (epoch - now.Seconds () - meanAnomalyAtEpoch / meanMotion));
        }

        static Orbit Created (global::KSA.Orbit orbit)
        {
            return createdOrbits.Add (new Orbit (orbit) { created = true });
        }

        /// <summary>
        /// Remove an orbit created by <see cref="CreateFromElements"/> or
        /// <see cref="CreateFromPositionAndVelocity"/>.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the orbit was not created by a client.
        /// </remarks>
        [KRPCMethod]
        public void Remove ()
        {
            if (!created)
                throw new InvalidOperationException ("The orbit was not created by a client");
            createdOrbits.Remove (this);
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
        /// The <a href="https://en.wikipedia.org/wiki/Conic_section#Conic_parameters">semi-latus
        /// rectum</a> of the orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double SemiLatusRectum {
            get { return InternalOrbit.SemiLatusRectum (); }
        }

        /// <summary>
        /// The specific angular momentum of the orbit, in <unit>m^2/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double SpecificAngularMomentum {
            get {
                var orbit = InternalOrbit;
                return Math.Sqrt (orbit.Mu * orbit.SemiLatusRectum ());
            }
        }

        /// <summary>
        /// The orbital speed at the given time, in meters per second.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        [KRPCMethod]
        public double SpeedAt (double ut)
        {
            return InternalOrbit.GetVelocityCciAt (new UniverseTime (ut)).Length ();
        }

        /// <summary>
        /// The mean anomaly at the given time, in radians.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        [KRPCMethod]
        public double MeanAnomalyAtUT (double ut)
        {
            var orbit = InternalOrbit;
            return Radians (orbit.GetMeanAnomaly (orbit.GetStateVectorsAt (new UniverseTime (ut)).TrueAnomaly).Degrees);
        }

        /// <summary>
        /// The eccentric anomaly at the given time, in radians. For a hyperbolic orbit, this
        /// is the hyperbolic anomaly.
        /// </summary>
        /// <param name="ut">Universal time, in seconds.</param>
        [KRPCMethod]
        public double EccentricAnomalyAtUT (double ut)
        {
            var orbit = InternalOrbit;
            return Radians (orbit.GetEccentricAnomaly (orbit.GetStateVectorsAt (new UniverseTime (ut)).TrueAnomaly).Degrees);
        }

        /// <summary>
        /// The true anomaly at which the orbit reaches the given distance from the center of
        /// the body, in radians, between 0 and <math>\pi</math>. Not a number if the orbit
        /// never reaches it.
        /// </summary>
        /// <param name="radius">The distance from the center of the body, in meters.</param>
        /// <remarks>
        /// The orbit also reaches the distance at the negative of this anomaly. Zero for a
        /// circular orbit at the distance. Throws an exception if the distance is not
        /// positive.
        /// </remarks>
        [KRPCMethod]
        public double TrueAnomalyAtRadius (double radius)
        {
            if (!(radius > 0))
                throw new ArgumentException ("The radius is not positive");
            var orbit = InternalOrbit;
            var semiLatusRectum = orbit.SemiLatusRectum ();
            // A circular orbit reaches its radius at every anomaly
            if (orbit.Eccentricity < 1e-9)
                return Math.Abs (radius - semiLatusRectum) <= 1e-9 * semiLatusRectum ? 0 : double.NaN;
            var cosine = (semiLatusRectum / radius - 1) / orbit.Eccentricity;
            // Rounding takes the cosine just past 1 at an apsis
            if (Math.Abs (cosine) > 1 + 1e-9)
                return double.NaN;
            return Math.Acos (Math.Clamp (cosine, -1, 1));
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
