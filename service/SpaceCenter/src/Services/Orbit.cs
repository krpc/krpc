using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Describes an orbit. For example, the orbit of a vessel, obtained by calling
    /// <see cref="Vessel.Orbit"/>, or a celestial body, obtained by calling
    /// <see cref="CelestialBody.Orbit"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Orbit : Equatable<Orbit>, IGameObjectState
    {
        readonly Vessel ownerVessel;
        readonly CelestialBody ownerBody;
        readonly Node ownerNode;
        // The KSP orbit, held only where it is not the orbit of the thing the object
        // belongs to and so cannot be asked for again: a patch, or an orbit a caller
        // handed in. An orbit that has an owner is looked up on it instead, because the
        // game builds a new one whenever it builds the owner, and the object has to read
        // the loaded game rather than the state it was made in.
        readonly global::Orbit patch;
        // Whether the client that constructed the orbit has gone. A constructed orbit is
        // as valid on the last frame of the session as on the first, so it stands for
        // nothing the game can destroy, and its client going is the only thing that can
        // say the object is finished with.
        bool released;

        internal Orbit (Vessel vessel)
        {
            if (ReferenceEquals (vessel, null))
                throw new ArgumentNullException (nameof (vessel));
            ownerVessel = vessel;
        }

        internal Orbit (CelestialBody body)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (body.InternalBody == body.InternalBody.referenceBody)
                throw new ArgumentException ("Body does not orbit anything");
            ownerBody = body;
        }

        internal Orbit (Node node)
        {
            if (ReferenceEquals (node, null))
                throw new ArgumentNullException (nameof (node));
            ownerNode = node;
        }

        /// <summary>
        /// Construct a an orbit from a KSP orbit object.
        /// </summary>
        public Orbit (global::Orbit orbit)
        {
            if (ReferenceEquals (orbit, null))
                throw new ArgumentNullException (nameof (orbit));
            patch = orbit;
        }

        // Construct an orbit from a KSP orbit object, inheriting the owner of an
        // existing orbit (used for a patch that follows a sphere-of-influence change,
        // which belongs to the same object).
        internal Orbit (global::Orbit orbit, Orbit owner)
        {
            patch = orbit;
            ownerVessel = owner.ownerVessel;
            ownerBody = owner.ownerBody;
            ownerNode = owner.ownerNode;
        }

        /// <summary>
        /// What the game holds for the thing the orbit belongs to. An orbit built from a KSP
        /// orbit alone has no owner to ask, so it is kept, and an orbit constructed for a
        /// client is kept until that client goes.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (released)
                    return GameObjectState.Destroyed;
                if (ownerVessel != null)
                    return ownerVessel.GameObjectState;
                if (ownerNode != null)
                    return ownerNode.GameObjectState;
                return GameObjectState.Live;
            }
        }

        /// <summary>
        /// Let go of an orbit constructed for a client that has gone, so that it and the
        /// reference frames defined against it leave the object store at the next sweep.
        /// </summary>
        internal void Release ()
        {
            released = true;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        /// <remarks>
        /// What the object stands for is the thing whose orbit it is, so two objects for
        /// one vessel's orbit are the same orbit however often the game rebuilds it. Only
        /// an orbit with no owner to name it by, a patch or one a caller handed in, is
        /// named by the KSP orbit itself. Neither may look anything up: the object store
        /// compares and hashes the objects it holds, including while removing one whose
        /// owner the game no longer has.
        /// </remarks>
        public override bool Equals (Orbit other)
        {
            if (ReferenceEquals (other, null))
                return false;
            if (!ReferenceEquals (patch, null) || !ReferenceEquals (other.patch, null))
                return ReferenceEquals (patch, other.patch);
            return ownerVessel == other.ownerVessel && ownerBody == other.ownerBody &&
            ownerNode == other.ownerNode;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            if (!ReferenceEquals (patch, null))
                return RuntimeHelpers.GetHashCode (patch);
            return Hash.Of (ownerVessel).And (ownerBody).And (ownerNode);
        }

        /// <summary>
        /// The KSP orbit object, asked of the thing the orbit belongs to on each use, so
        /// that the object keeps working across a load that rebuilds that thing and its
        /// orbit with it. A patch, and an orbit a caller handed in, have nothing to be
        /// asked of and are held.
        /// </summary>
        public global::Orbit InternalOrbit {
            get {
                if (!ReferenceEquals (patch, null))
                    return patch;
                if (ownerVessel != null)
                    return ownerVessel.InternalVessel.GetOrbit ();
                if (ownerBody != null)
                    return ownerBody.InternalBody.GetOrbit ();
                return ownerNode.InternalNode.nextPatch;
            }
        }

        /// <summary>
        /// The celestial body (e.g. planet or moon) around which the object is orbiting.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body {
            get { return SpaceCenter.Bodies [InternalOrbit.referenceBody.name]; }
        }

        /// <summary>
        /// The reference frame that moves along the orbit, and is orientated in a fixed
        /// direction.
        /// <list type="bullet">
        /// <item><description>The origin is at the point the orbit has reached at the
        /// current time.</description></item>
        /// <item><description>The axes do not rotate. They point in the same fixed
        /// directions as those of
        /// <see cref="CelestialBody.NonRotatingReferenceFrame"/> of
        /// <see cref="Body"/>.</description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// For the orbit of a vessel, the origin is the point the vessel's orbit has
        /// reached rather than the vessel's own center of mass. The two are a few meters
        /// apart, as a vessel inside the physics bubble is simulated rather than being
        /// held on its orbit.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.NonRotating (this); }
        }

        /// <summary>
        /// The reference frame that moves along the orbit, and is orientated with its
        /// prograde/normal/radial directions.
        /// <list type="bullet">
        /// <item><description>The origin is at the point the orbit has reached at the
        /// current time.</description></item>
        /// <item><description>The axes rotate with the orbital prograde/normal/radial
        /// directions.</description></item>
        /// <item><description>The x-axis points in the orbital anti-radial direction.
        /// </description></item>
        /// <item><description>The y-axis points in the orbital prograde direction.
        /// </description></item>
        /// <item><description>The z-axis points in the orbital normal direction.
        /// </description></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// For the orbit of a vessel, the origin is the point the vessel's orbit has
        /// reached rather than the vessel's own center of mass, which is what
        /// <see cref="Vessel.OrbitalReferenceFrame"/> is centered on.
        /// </remarks>
        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (this); }
        }

        // The reference frame used by the closest-approach members when the caller
        // does not specify one: the orbital frame of the object the orbit belongs to,
        // falling back to the reference body's non-rotating (inertial) frame when the
        // owner is unknown.
        internal ReferenceFrame DefaultReferenceFrame {
            get {
                if (ownerNode != null)
                    return ownerNode.OrbitalReferenceFrame;
                if (ownerVessel != null)
                    return ownerVessel.OrbitalReferenceFrame;
                if (ownerBody != null)
                    return ownerBody.OrbitalReferenceFrame;
                return Body.NonRotatingReferenceFrame;
            }
        }

        // The vessel this orbit belongs to, or null if it belongs to something else or
        // the owner is unknown.
        internal Vessel OwnerVessel {
            get { return ownerVessel; }
        }

        // The celestial body this orbit belongs to (the orbiting body, not the parent
        // it orbits — that is Body), or null if it belongs to something else or the
        // owner is unknown.
        internal CelestialBody OwnerBody {
            get { return ownerBody; }
        }

        /// <summary>
        /// Gets the apoapsis of the orbit, in meters, from the center of mass
        /// of the body being orbited.
        /// </summary>
        /// <remarks>
        /// For the apoapsis altitude reported on the in-game map view,
        /// use <see cref="ApoapsisAltitude"/>.
        /// </remarks>
        [KRPCProperty]
        public double Apoapsis {
            get { return InternalOrbit.ApR; }
        }

        /// <summary>
        /// The periapsis of the orbit, in meters, from the center of mass
        /// of the body being orbited.
        /// </summary>
        /// <remarks>
        /// For the periapsis altitude reported on the in-game map view,
        /// use <see cref="PeriapsisAltitude"/>.
        /// </remarks>
        [KRPCProperty]
        public double Periapsis {
            get { return InternalOrbit.PeR; }
        }

        /// <summary>
        /// The apoapsis of the orbit, in meters, above the sea level of the body being orbited.
        /// </summary>
        /// <remarks>
        /// This is equal to <see cref="Apoapsis"/> minus the equatorial radius of the body.
        /// </remarks>
        [KRPCProperty]
        public double ApoapsisAltitude {
            get { return InternalOrbit.ApA; }
        }

        /// <summary>
        /// The periapsis of the orbit, in meters, above the sea level of the body being orbited.
        /// </summary>
        /// <remarks>
        /// This is equal to <see cref="Periapsis"/> minus the equatorial radius of the body.
        /// </remarks>
        [KRPCProperty]
        public double PeriapsisAltitude {
            get { return InternalOrbit.PeA; }
        }

        /// <summary>
        /// The semi-major axis of the orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double SemiMajorAxis {
            get { return 0.5d * (Apoapsis + Periapsis); }
        }

        /// <summary>
        /// The semi-minor axis of the orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double SemiMinorAxis {
            get {
                var e = Eccentricity;
                return SemiMajorAxis * Math.Sqrt (1d - (e * e));
            }
        }

        /// <summary>
        /// The current radius of the orbit, in meters. This is the distance between the center
        /// of mass of the object in orbit, and the center of mass of the body around which it
        /// is orbiting.
        /// </summary>
        /// <remarks>
        /// This value will change over time if the orbit is elliptical.
        /// </remarks>
        [KRPCProperty]
        public double Radius {
            get { return InternalOrbit.radius; }
        }

        /// <summary>
        /// The current orbital speed of the object in meters per second.
        /// </summary>
        /// <remarks>
        /// This value will change over time if the orbit is elliptical.
        /// </remarks>
        [KRPCProperty]
        public double Speed {
            get { return InternalOrbit.vel.magnitude; }
        }

        /// <summary>
        /// The orbital period, in seconds.
        /// </summary>
        [KRPCProperty]
        public double Period {
            get { return InternalOrbit.period; }
        }

        /// <summary>
        /// The time until the object reaches apoapsis, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeToApoapsis {
            get { return InternalOrbit.timeToAp; }
        }

        /// <summary>
        /// The time until the object reaches periapsis, in seconds.
        /// </summary>
        [KRPCProperty]
        public double TimeToPeriapsis {
            get { return InternalOrbit.timeToPe; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_eccentricity">eccentricity</a>
        /// of the orbit.
        /// </summary>
        [KRPCProperty]
        public double Eccentricity {
            get { return InternalOrbit.eccentricity; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_inclination">inclination</a>
        /// of the orbit,
        /// in radians.
        /// </summary>
        [KRPCProperty]
        public double Inclination {
            get { return GeometryExtensions.ToRadians (InternalOrbit.inclination); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Longitude_of_the_ascending_node">longitude of
        /// the ascending node</a>, in radians.
        /// </summary>
        /// <remarks>
        /// For a near-equatorial orbit, the ascending node is ill-defined and
        /// this value may vary erratically over time.
        /// </remarks>
        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return GeometryExtensions.ToRadians (InternalOrbit.LAN); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Argument_of_periapsis">argument of
        /// periapsis</a>, in radians.
        /// </summary>
        /// <remarks>
        /// For a near-circular orbit, the periapsis is ill-defined and
        /// this value may vary erratically over time.
        /// </remarks>
        [KRPCProperty]
        public double ArgumentOfPeriapsis {
            get { return GeometryExtensions.ToRadians (InternalOrbit.argumentOfPeriapsis); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly at epoch</a>.
        /// </summary>
        [KRPCProperty]
        public double MeanAnomalyAtEpoch {
            get { return InternalOrbit.meanAnomalyAtEpoch; }
        }

        /// <summary>
        /// The universal time, in seconds, at which the
        /// <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly at epoch</a>
        /// is measured.
        /// </summary>
        [KRPCProperty]
        public double Epoch {
            get { return InternalOrbit.epoch; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly</a>.
        /// </summary>
        [KRPCProperty]
        public double MeanAnomaly {
            get { return InternalOrbit.meanAnomaly; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Eccentric_anomaly">eccentric anomaly</a>.
        /// </summary>
        [KRPCProperty]
        public double EccentricAnomaly {
            get { return InternalOrbit.eccentricAnomaly; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/True_anomaly">true anomaly</a>.
        /// </summary>
        [KRPCProperty]
        public double TrueAnomaly {
            get { return InternalOrbit.trueAnomaly; }
        }

        /// <summary>
        /// Create the orbit that passes through a given position at a given velocity.
        /// The orbit coasts freely under the gravity of the body being orbited, so it
        /// describes where an object left to fall from that position and velocity would
        /// be at any later time.
        /// </summary>
        /// <param name="body">The celestial body being orbited.</param>
        /// <param name="position">The position, as a position vector.</param>
        /// <param name="velocity">The velocity, as a vector pointing in the direction of
        /// travel, whose magnitude is the speed in meters per second.</param>
        /// <param name="ut">The universal time, in seconds, at which the object is at
        /// <paramref name="position"/> traveling at <paramref name="velocity"/>.</param>
        /// <param name="referenceFrame">The reference frame that
        /// <paramref name="position"/> and <paramref name="velocity"/> are in. Defaults to
        /// <see cref="CelestialBody.NonRotatingReferenceFrame"/> of
        /// <paramref name="body"/>.</param>
        /// <remarks>
        /// The orbit is a single conic around <paramref name="body"/>. Nothing else acts on
        /// it: it never changes sphere of influence, so <see cref="NextOrbit"/> is
        /// <c>null</c> however far it travels from the body, and it is not slowed by an
        /// atmosphere. A vessel held on rails follows its own orbit exactly, while one
        /// inside the physics bubble is simulated and drifts from it a little.
        ///
        /// The members that describe where the orbit has got to -- <see cref="Radius"/>,
        /// <see cref="Speed"/>, <see cref="TrueAnomaly"/>, <see cref="TimeToApoapsis"/>
        /// and the like -- describe it at <paramref name="ut"/> and stay there, as
        /// nothing is moving along it. Use <see cref="RadiusAt"/>,
        /// <see cref="PositionAt"/>, <see cref="VelocityAt"/> and
        /// <see cref="TrueAnomalyAtUT"/> to ask where it is at another time.
        /// <see cref="ReferenceFrame"/> and <see cref="OrbitalReferenceFrame"/> follow
        /// the orbit as time passes.
        ///
        /// The orbit that is returned is kept for as long as the server is running,
        /// so creating one repeatedly, for example once per update, uses more and more
        /// memory.
        /// </remarks>
        [KRPCMethod]
        public static Orbit CreateFromPositionAndVelocity (
            CelestialBody body, Tuple3 position, Tuple3 velocity, double ut,
            ReferenceFrame referenceFrame = null)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (position == null)
                throw new ArgumentNullException (nameof (position));
            if (velocity == null)
                throw new ArgumentNullException (nameof (velocity));
            var internalBody = body.InternalBody;
            var frame = referenceFrame ?? body.NonRotatingReferenceFrame;
            var framePosition = position.ToVector ();
            var worldPosition = frame.PositionToWorldSpace (framePosition);
            var worldVelocity = frame.VelocityToWorldSpace (framePosition, velocity.ToVector ());
            // The game states an orbit relative to the body it is around, in its own axis
            // order, so take out the body's own position and motion and swap the axes.
            var relativePosition = worldPosition - internalBody.position;
            var relativeVelocity = worldVelocity - internalBody.GetWorldVelocity ();
            if (relativePosition.sqrMagnitude < 1)
                throw new ArgumentException (
                    "Position is at the center of " + internalBody.name +
                    ", which does not describe an orbit");
            var orbit = new global::Orbit ();
            orbit.UpdateFromStateVectors (
                relativePosition.SwapYZ (), relativeVelocity.SwapYZ (), internalBody, ut);
            // Init derives the mean motion, period and anomalies that the orbit is
            // propagated from; without it the orbit only carries its shape. Stepping it
            // to the epoch then fills in where along the orbit it has got to, which
            // nothing else will do for an orbit that no object is following.
            orbit.Init ();
            orbit.UpdateFromUT (ut);
            if (!IsFinite (orbit.semiMajorAxis) || !IsFinite (orbit.eccentricity))
                throw new ArgumentException (
                    "Position and velocity do not describe an orbit around " +
                    internalBody.name);
            // Nothing solved a following patch for this orbit, and a sphere-of-influence
            // change is reported as one in the past. Zero, the value an orbit is built
            // with, is not in the past at a universal time of zero.
            orbit.UTsoi = -1;
            return Constructed (orbit);
        }

        /// <summary>
        /// Create the orbit with the given
        /// <a href="https://en.wikipedia.org/wiki/Orbital_elements">orbital elements</a>
        /// around a given body. The orbit coasts freely under the gravity of that body,
        /// so it describes where an object on it would be at any time.
        /// </summary>
        /// <param name="body">The celestial body being orbited.</param>
        /// <param name="semiMajorAxis">The semi-major axis of the orbit, in meters.
        /// Positive for an ellipse and negative for a hyperbola.</param>
        /// <param name="eccentricity">The eccentricity of the orbit. Below one for an
        /// ellipse and above one for a hyperbola.</param>
        /// <param name="inclination">The inclination of the orbit, in radians.</param>
        /// <param name="longitudeOfAscendingNode">The longitude of the ascending node,
        /// in radians.</param>
        /// <param name="argumentOfPeriapsis">The argument of periapsis, in
        /// radians.</param>
        /// <param name="meanAnomalyAtEpoch">The mean anomaly at
        /// <paramref name="epoch"/>, in radians.</param>
        /// <param name="epoch">The universal time, in seconds, that
        /// <paramref name="meanAnomalyAtEpoch"/> is measured at.</param>
        /// <remarks>
        /// The orbit is a single conic around <paramref name="body"/>. Nothing else acts
        /// on it: it never changes sphere of influence, so <see cref="NextOrbit"/> is
        /// <c>null</c> however far it travels from the body, and it is not slowed by an
        /// atmosphere.
        ///
        /// The angles are measured against the same reference plane and direction that
        /// <see cref="Inclination"/>, <see cref="LongitudeOfAscendingNode"/> and
        /// <see cref="ArgumentOfPeriapsis"/> report them against, which
        /// <see cref="ReferencePlaneNormal"/> and <see cref="ReferencePlaneDirection"/>
        /// give as vectors in a reference frame.
        ///
        /// The members that describe where the orbit has got to -- <see cref="Radius"/>,
        /// <see cref="Speed"/>, <see cref="TrueAnomaly"/>, <see cref="TimeToApoapsis"/>
        /// and the like -- describe it at <paramref name="epoch"/> and stay there, as
        /// nothing is moving along it. Use <see cref="RadiusAt"/>,
        /// <see cref="PositionAt"/>, <see cref="VelocityAt"/> and
        /// <see cref="TrueAnomalyAtUT"/> to ask where it is at another time.
        /// <see cref="ReferenceFrame"/> and <see cref="OrbitalReferenceFrame"/> follow
        /// the orbit as time passes.
        ///
        /// The orbit that is returned is kept for as long as the server is running,
        /// so creating one repeatedly, for example once per update, uses more and more
        /// memory.
        /// </remarks>
        [KRPCMethod]
        public static Orbit CreateFromOrbitalElements (
            CelestialBody body, double semiMajorAxis, double eccentricity,
            double inclination, double longitudeOfAscendingNode,
            double argumentOfPeriapsis, double meanAnomalyAtEpoch, double epoch)
        {
            if (ReferenceEquals (body, null))
                throw new ArgumentNullException (nameof (body));
            if (!IsFinite (semiMajorAxis) || !IsFinite (eccentricity) ||
                !IsFinite (inclination) || !IsFinite (longitudeOfAscendingNode) ||
                !IsFinite (argumentOfPeriapsis) || !IsFinite (meanAnomalyAtEpoch) ||
                !IsFinite (epoch))
                throw new ArgumentException ("Orbital elements must be finite");
            if (eccentricity < 0)
                throw new ArgumentException (
                    "Eccentricity must not be negative, got " + eccentricity);
            // A conic is an ellipse with a positive semi-major axis, or a hyperbola with
            // a negative one. The parabola between them has neither, and the game has no
            // way to state it.
            if (eccentricity.Equals (1.0))
                throw new ArgumentException (
                    "An eccentricity of exactly one is a parabola, which has no " +
                    "semi-major axis and cannot be described as an orbit");
            if (eccentricity < 1 && semiMajorAxis <= 0)
                throw new ArgumentException (
                    "An eccentricity below one is an ellipse, which needs a positive " +
                    "semi-major axis, got " + semiMajorAxis);
            if (eccentricity > 1 && semiMajorAxis >= 0)
                throw new ArgumentException (
                    "An eccentricity above one is a hyperbola, which needs a negative " +
                    "semi-major axis, got " + semiMajorAxis);
            // The game states the three orientation angles in degrees, and the mean
            // anomaly in radians, which is what this reports them in.
            var orbit = new global::Orbit (
                GeometryExtensions.ToDegrees (inclination), eccentricity, semiMajorAxis,
                GeometryExtensions.ToDegrees (longitudeOfAscendingNode),
                GeometryExtensions.ToDegrees (argumentOfPeriapsis), meanAnomalyAtEpoch,
                epoch, body.InternalBody);
            // The constructor derives the mean motion, period and anomalies that the
            // orbit is propagated from. Stepping it to the epoch then fills in where
            // along the orbit it has got to, which nothing else will do for an orbit
            // that no object is following.
            orbit.UpdateFromUT (epoch);
            // Nothing solved a following patch for this orbit, and a sphere-of-influence
            // change is reported as one in the past. Zero, the value an orbit is built
            // with, is not in the past at a universal time of zero.
            orbit.UTsoi = -1;
            return Constructed (orbit);
        }

        // The object for an orbit a client asked to be built, recorded as the client's so
        // that it is let go of when the client is. An orbit read off a vessel, a body or a
        // maneuver node is not recorded: it is named by the thing whose orbit it is, so
        // the object store gives back one object for it however often it is asked for,
        // and dropping it when one client goes would take it away from the others.
        static Orbit Constructed (global::Orbit orbit)
        {
            var result = new Orbit (orbit);
            ConstructedOrbitsAddon.Add (result);
            return result;
        }

        static bool IsFinite (double value)
        {
            return !double.IsNaN (value) && !double.IsInfinity (value);
        }

        /// <summary>
        /// The direction that is normal to the orbits reference plane,
        /// in the given reference frame.
        /// The reference plane is the plane from which the orbits inclination is measured.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public static Tuple3 ReferencePlaneNormal (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (Planetarium.up).normalized.ToTuple ();
        }

        /// <summary>
        /// The direction from which the orbits longitude of ascending node is measured,
        /// in the given reference frame.
        /// </summary>
        /// <returns>The direction as a unit vector.</returns>
        /// <param name="referenceFrame">The reference frame that the returned
        /// direction is in.</param>
        [KRPCMethod]
        public static Tuple3 ReferencePlaneDirection (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (Planetarium.right).normalized.ToTuple ();
        }

        /// <summary>
        /// If the object is going to change sphere of influence in the future, returns the new
        /// orbit after the change. Otherwise returns <c>null</c>.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Orbit NextOrbit {
            get { return (double.IsNaN (TimeToSOIChange)) ? null : new Orbit (InternalOrbit.nextPatch, this); }
        }

        /// <summary>
        /// The time until the object changes sphere of influence, in seconds. Returns <c>NaN</c>
        /// if the object is not going to change sphere of influence.
        /// </summary>
        [KRPCProperty]
        public double TimeToSOIChange {
            get {
                var time = InternalOrbit.UTsoi - SpaceCenter.UT;
                return time < 0 ? double.NaN : time;
            }
        }

        /// <summary>
        /// The mean anomaly at the given time.
        /// </summary>
        /// <param name="ut">The universal time in seconds.</param>
        [KRPCMethod]
        public double MeanAnomalyAtUT(double ut)
        {
            var percent = InternalOrbit.getObtAtUT(ut) / InternalOrbit.period;
            return percent * (2 * Math.PI);
        }

        /// <summary>
        /// The orbital radius at the point in the orbit given by the true anomaly.
        /// </summary>
        /// <param name="trueAnomaly">The true anomaly.</param>
        [KRPCMethod]
        public double RadiusAtTrueAnomaly (double trueAnomaly)
        {
            return InternalOrbit.RadiusAtTrueAnomaly (trueAnomaly);
        }

        /// <summary>
        /// The true anomaly at the given orbital radius.
        /// </summary>
        /// <param name="radius">The orbital radius in meters.</param>
        [KRPCMethod]
        public double TrueAnomalyAtRadius (double radius)
        {
            return InternalOrbit.TrueAnomalyAtRadius (radius);
        }

        /// <summary>
        /// The true anomaly at the given time.
        /// </summary>
        /// <param name="ut">The universal time in seconds.</param>
        [KRPCMethod]
        public double TrueAnomalyAtUT (double ut)
        {
            return InternalOrbit.TrueAnomalyAtUT (ut);
        }

        /// <summary>
        /// The universal time, in seconds, corresponding to the given true anomaly.
        /// </summary>
        /// <param name="trueAnomaly">True anomaly.</param>
        [KRPCMethod]
        public double UTAtTrueAnomaly (double trueAnomaly)
        {
            return InternalOrbit.GetUTforTrueAnomaly (trueAnomaly, 0);
        }

        /// <summary>
        /// The eccentric anomaly at the given universal time.
        /// </summary>
        /// <param name="ut">The universal time, in seconds.</param>
        [KRPCMethod]
        public double EccentricAnomalyAtUT (double ut)
        {
            return InternalOrbit.EccentricAnomalyAtUT (ut);
        }

        /// <summary>
        /// The current orbital speed in meters per second.
        /// </summary>
        [KRPCProperty]
        public double OrbitalSpeed {
            get { return InternalOrbit.orbitalSpeed; }
        }

        /// <summary>
        /// The orbital speed at the given time, in meters per second.
        /// </summary>
        /// <param name="time">Time from now, in seconds.</param>
        [KRPCMethod]
        public double OrbitalSpeedAt (double time)
        {
            return InternalOrbit.getOrbitalSpeedAt (time);
        }

        /// <summary>
        /// The specific orbital energy of the orbit, in Joules per kilogram
        /// (equivalently, meters squared per second squared). This is the sum
        /// of the orbit's specific kinetic and potential energy, and is
        /// negative for a bound (elliptical) orbit.
        /// </summary>
        [KRPCProperty]
        public double OrbitalEnergy {
            get { return InternalOrbit.orbitalEnergy; }
        }

        /// <summary>
        /// The orbital radius at the given time, in meters.
        /// </summary>
        /// <param name="ut">The universal time to measure the radius at.</param>
        [KRPCMethod]
        public double RadiusAt (double ut)
        {
            return InternalOrbit.getRelativePositionAtUT(ut).magnitude;
        }

        /// <summary>
        /// The position at a given time, in the specified reference frame.
        /// </summary>
        /// <returns>The position as a vector.</returns>
        /// <param name="ut">The universal time to measure the position at.</param>
        /// <param name="referenceFrame">The reference frame that the returned
        /// position vector is in.</param>
        [KRPCMethod]
        public Tuple3 PositionAt (double ut, ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace(InternalOrbit.getPositionAtUT(ut)).ToTuple();
        }

        /// <summary>
        /// The velocity at a given time, in the specified reference frame.
        /// </summary>
        /// <returns>The velocity as a vector. The vector points in the direction of
        /// travel, and its magnitude is the speed in meters per second.</returns>
        /// <param name="ut">The universal time to measure the velocity at.</param>
        /// <param name="referenceFrame">The reference frame that the returned
        /// velocity vector is in.</param>
        [KRPCMethod]
        public Tuple3 VelocityAt (double ut, ReferenceFrame referenceFrame)
        {
            // An orbital velocity is relative to the body being orbited, so the body's
            // own motion is added to it to give a velocity in world space.
            var worldVelocity =
                InternalOrbit.getOrbitalVelocityAtUT (ut).SwapYZ () +
                InternalOrbit.referenceBody.GetWorldVelocity ();
            return referenceFrame.VelocityFromWorldSpace (
                InternalOrbit.getPositionAtUT (ut), worldVelocity).ToTuple ();
        }

        /// <summary>
        /// The next closest approach to a target orbit.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public ClosestApproach NextClosestApproach (Orbit target)
        {
            return new ClosestApproach (this, target, Planetarium.GetUniversalTime ());
        }

        /// <summary>
        /// A list of the closest approaches to a target orbit, one for each of the next
        /// <paramref name="orbits"/> orbital periods.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        /// <param name="orbits">The number of future orbits to search.</param>
        [KRPCMethod]
        public IList<ClosestApproach> ClosestApproaches (Orbit target, int orbits)
        {
            var approaches = new List<ClosestApproach> ();
            double orbitstart = Planetarium.GetUniversalTime ();
            double period = InternalOrbit.period;
            for (int i = 0; i < orbits; i++) {
                approaches.Add (new ClosestApproach (this, target, orbitstart));
                orbitstart += period;
            }
            return approaches;
        }

        /// <summary>
        /// Estimates and returns the time at closest approach to a target orbit.
        /// </summary>
        /// <returns>The universal time at closest approach, in seconds.</returns>
        /// <param name="target">Target orbit.</param>
        [Obsolete ("Use <see cref='NextClosestApproach'/> and read <see cref='ClosestApproach.UT'/> instead.")]
        [KRPCMethod]
        public double TimeOfClosestApproach (Orbit target)
        {
            double distance;
            return CalcClosestAproach(this, target, Planetarium.GetUniversalTime(), out distance);
        }

        /// <summary>
        /// Estimates and returns the distance at closest approach to a target orbit, in meters.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [Obsolete ("Use <see cref='NextClosestApproach'/> and read <see cref='ClosestApproach.Distance'/> instead.")]
        [KRPCMethod]
        public double DistanceAtClosestApproach (Orbit target)
        {
            double distance;
            CalcClosestAproach(this, target, Planetarium.GetUniversalTime(), out distance);
            return distance;
        }

        /// <summary>
        /// Returns the times at closest approach and corresponding distances, to a target orbit.
        /// </summary>
        /// <returns>
        /// A list of two lists.
        /// The first is a list of times at closest approach, as universal times in seconds.
        /// The second is a list of corresponding distances at closest approach, in meters.
        /// </returns>
        /// <param name="target">Target orbit.</param>
        /// <param name="orbits">The number of future orbits to search.</param>
        [Obsolete ("Use <see cref='ClosestApproaches'/> instead.")]
        [KRPCMethod]
        public IList<IList<double>> ListClosestApproaches(Orbit target, int orbits)
        {
            var times = new List<double>();
            var distances = new List<double>();
            double distance;
            double orbitstart = Planetarium.GetUniversalTime();
            double period = InternalOrbit.period;
            for (int i = 0; i < orbits; i++) {
                times.Add(CalcClosestAproach(this, target, orbitstart, out distance));
                distances.Add(distance);
                orbitstart += period;
            }
            var combined = new List<IList<double>>();
            combined.Add(times);
            combined.Add(distances);
            return combined;
        }

        /// <summary>
        /// Helper function to calculate the closest approach distance and time to a target orbit
        /// in a given orbital period.
        /// </summary>
        /// <param name="myOrbit">Orbit of the controlled vessel.</param>
        /// <param name="targetOrbit">Orbit of the target.</param>
        /// <param name="beginTime">Time to begin search, which continues for
        /// one orbital period from this time.</param>
        /// <param name="distance">The distance at the closest approach, in meters.</param>
        /// <returns>The universal time at closest approach, in seconds.</returns>
        public static double CalcClosestAproach(Orbit myOrbit, Orbit targetOrbit, double beginTime, out double distance)
        {
            if (ReferenceEquals (myOrbit, null))
                throw new ArgumentNullException (nameof (myOrbit));
            if (ReferenceEquals (targetOrbit, null))
                throw new ArgumentNullException (nameof (targetOrbit));
            double approachTime = beginTime;
            double approachDistance = double.MaxValue;
            double mintime = beginTime;
            double interval = myOrbit.Period;
            if (myOrbit.Eccentricity > 1.0)
                interval = 100 / myOrbit.InternalOrbit.meanMotion;
            double maxtime = mintime + interval;

            // Conduct coarse search
            double timestep = (maxtime - mintime) / 20;
            double placeholder = mintime;
            while (placeholder < maxtime) {
                Vector3d PosA = myOrbit.InternalOrbit.getPositionAtUT(placeholder);
                Vector3d PosB = targetOrbit.InternalOrbit.getPositionAtUT(placeholder);
                double thisDistance = Vector3d.Distance(PosA, PosB);
                if (thisDistance < approachDistance) {
                    approachDistance = thisDistance;
                    approachTime = placeholder;
                }
                placeholder += timestep;
            }

            // Conduct fine search
            double fine_mintime = approachTime - timestep;
            double fine_maxtime = approachTime + timestep;
            if (fine_maxtime > maxtime) fine_maxtime = maxtime;
            if (fine_mintime<mintime) fine_mintime = mintime;
            timestep = (fine_maxtime - fine_mintime) / 50;
            placeholder = fine_mintime;

            while (placeholder < fine_maxtime) {
                Vector3d PosA = myOrbit.InternalOrbit.getPositionAtUT(placeholder);
                Vector3d PosB = targetOrbit.InternalOrbit.getPositionAtUT(placeholder);
                double thisDistance = Vector3d.Distance(PosA, PosB);
                if (thisDistance < approachDistance) {
                    approachDistance = thisDistance;
                    approachTime = placeholder;
                }
                placeholder += timestep;
            }
            distance = approachDistance;
            return approachTime;
        }

        /// <summary>
        /// The true anomaly of the ascending node with the given target orbit.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public double TrueAnomalyAtAN(Orbit target)
        {
            var degrees = FinePrint.Utilities.OrbitUtilities.AngleOfAscendingNode(InternalOrbit, target.InternalOrbit);
            return GeometryExtensions.ToRadians (GeometryExtensions.ClampAngle180 (degrees));
        }

        /// <summary>
        /// The true anomaly of the descending node with the given target orbit.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public double TrueAnomalyAtDN(Orbit target)
        {
            var degrees = FinePrint.Utilities.OrbitUtilities.AngleOfDescendingNode(InternalOrbit, target.InternalOrbit);
            return GeometryExtensions.ToRadians (GeometryExtensions.ClampAngle180 (degrees));
        }

        /// <summary>
        /// Relative inclination of this orbit and the target orbit, in radians.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public double RelativeInclination(Orbit target)
        {
            var degrees = FinePrint.Utilities.OrbitUtilities.GetRelativeInclination(InternalOrbit, target.InternalOrbit);
            return GeometryExtensions.ToRadians(degrees);
        }
    }
}
