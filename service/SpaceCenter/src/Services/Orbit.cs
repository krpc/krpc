using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Describes an orbit. For example, the orbit of a vessel, obtained by calling
    /// <see cref="Vessel.Orbit"/>, or a celestial body, obtained by calling
    /// <see cref="CelestialBody.Orbit"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Orbit : Equatable<Orbit>
    {
        internal Orbit (global::Vessel vessel)
        {
            InternalOrbit = vessel.GetOrbit ();
        }

        internal Orbit (global::CelestialBody body)
        {
            if (body == body.referenceBody)
                throw new ArgumentException ("Body does not orbit anything");
            InternalOrbit = body.GetOrbit ();
        }

        /// <summary>
        /// Construct a an orbit from a KSP orbit object.
        /// </summary>
        public Orbit (global::Orbit orbit)
        {
            InternalOrbit = orbit;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Orbit other)
        {
            return !ReferenceEquals (other, null) && InternalOrbit == other.InternalOrbit;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return InternalOrbit.GetHashCode ();
        }

        /// <summary>
        /// The KSP orbit object.
        /// </summary>
        public global::Orbit InternalOrbit { get; private set; }

        /// <summary>
        /// The celestial body (e.g. planet or moon) around which the object is orbiting.
        /// </summary>
        [KRPCProperty]
        public CelestialBody Body {
            get { return SpaceCenter.Bodies [InternalOrbit.referenceBody.name]; }
        }

        /// <summary>
        /// Gets the apoapsis of the orbit, in meters, from the center of mass of the body being orbited.
        /// </summary>
        /// <remarks>
        /// For the apoapsis altitude reported on the in-game map view, use <see cref="ApoapsisAltitude"/>.
        /// </remarks>
        [KRPCProperty]
        public double Apoapsis {
            get { return InternalOrbit.ApR; }
        }

        /// <summary>
        /// The periapsis of the orbit, in meters, from the center of mass of the body being orbited.
        /// </summary>
        /// <remarks>
        /// For the periapsis altitude reported on the in-game map view, use <see cref="PeriapsisAltitude"/>.
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
        /// of mass of the object in orbit, and the center of mass of the body around which it is orbiting.
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
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_eccentricity">eccentricity</a> of the orbit.
        /// </summary>
        [KRPCProperty]
        public double Eccentricity {
            get { return InternalOrbit.eccentricity; }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Orbital_inclination">inclination</a> of the orbit,
        /// in radians.
        /// </summary>
        [KRPCProperty]
        public double Inclination {
            get { return GeometryExtensions.ToRadians (InternalOrbit.inclination); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Longitude_of_the_ascending_node">longitude of the
        /// ascending node</a>, in radians.
        /// </summary>
        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return GeometryExtensions.ToRadians (InternalOrbit.LAN); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Argument_of_periapsis">argument of periapsis</a>, in radians.
        /// </summary>
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
        /// The time since the epoch (the point at which the
        /// <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly at epoch</a> was measured, in seconds.
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
        /// The unit direction vector that is normal to the orbits reference plane, in the given
        /// reference frame. The reference plane is the plane from which the orbits inclination is measured.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public static Tuple3 ReferencePlaneNormal (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (Planetarium.up).normalized.ToTuple ();
        }

        /// <summary>
        /// The unit direction vector from which the orbits longitude of ascending node is measured,
        /// in the given reference frame.
        /// </summary>
        /// <param name="referenceFrame"></param>
        [KRPCMethod]
        public static Tuple3 ReferencePlaneDirection (ReferenceFrame referenceFrame)
        {
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (Planetarium.right).normalized.ToTuple ();
        }

        /// <summary>
        /// If the object is going to change sphere of influence in the future, returns the new orbit
        /// after the change. Otherwise returns <c>null</c>.
        /// </summary>
        [KRPCProperty]
        public Orbit NextOrbit {
            get { return (double.IsNaN (TimeToSOIChange)) ? null : new Orbit (InternalOrbit.nextPatch); }
        }

        /// <summary>
        /// The time until the object changes sphere of influence, in seconds. Returns <c>NaN</c> if the
        /// object is not going to change sphere of influence.
        /// </summary>
        [KRPCProperty]
        public double TimeToSOIChange {
            get {
                var time = InternalOrbit.UTsoi - SpaceCenter.UT;
                return time < 0 ? double.NaN : time;
            }
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
    }
}
