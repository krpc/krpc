using System;
using System.Collections.Generic;
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
        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return GeometryExtensions.ToRadians (InternalOrbit.LAN); }
        }

        /// <summary>
        /// The <a href="https://en.wikipedia.org/wiki/Argument_of_periapsis">argument of
        /// periapsis</a>, in radians.
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
        /// <a href="https://en.wikipedia.org/wiki/Mean_anomaly">mean anomaly at epoch</a>
        /// was measured, in seconds.
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
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
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
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.DirectionFromWorldSpace (Planetarium.right).normalized.ToTuple ();
        }

        /// <summary>
        /// If the object is going to change sphere of influence in the future, returns the new
        /// orbit after the change. Otherwise returns <c>null</c>.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Orbit NextOrbit {
            get { return (double.IsNaN (TimeToSOIChange)) ? null : new Orbit (InternalOrbit.nextPatch); }
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
            if (ReferenceEquals (referenceFrame, null))
                throw new ArgumentNullException (nameof (referenceFrame));
            return referenceFrame.PositionFromWorldSpace(InternalOrbit.getPositionAtUT(ut)).ToTuple();
        }

        /// <summary>
        /// Estimates and returns the time at closest approach to a target orbit.
        /// </summary>
        /// <returns>The universal time at closest approach, in seconds.</returns>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public double TimeOfClosestApproach (Orbit target)
        {
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
            double distance;
            return CalcClosestAproach(this, target, Planetarium.GetUniversalTime(), out distance);
        }

        /// <summary>
        /// Estimates and returns the distance at closest approach to a target orbit, in meters.
        /// </summary>
        /// <param name="target">Target orbit.</param>
        [KRPCMethod]
        public double DistanceAtClosestApproach (Orbit target)
        {
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
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
        [KRPCMethod]
        public IList<IList<double>> ListClosestApproaches(Orbit target, int orbits)
        {
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
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
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
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
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
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
            if (ReferenceEquals (target, null))
                throw new ArgumentNullException (nameof (target));
            var degrees = FinePrint.Utilities.OrbitUtilities.GetRelativeInclination(InternalOrbit, target.InternalOrbit);
            return GeometryExtensions.ToRadians(degrees);
        }
    }
}
