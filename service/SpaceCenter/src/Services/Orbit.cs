using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using System.Collections.Generic;

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

        /// <summary>
        /// The orbital radius at the given time, in meters.
        /// </summary>
        /// <param name="time">UT time to measure radius at.</param>
        [KRPCMethod]
        public double RadiusAt(double time)
        {
            return InternalOrbit.getRelativePositionAtUT(time).magnitude;
        }

        /// <summary>
        /// The position at a given time, in the specified reference frame.
        /// </summary>
        /// <param name="time">UT time to measure position at.</param>
        /// <param name="referenceFrame">Reference Frame.</param>
        [KRPCMethod]
        public Tuple3 PositionAt(double time, ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace(InternalOrbit.getPositionAtUT(time)).ToTuple();

        }


        /// <summary>
        /// Estimates time of closest approach in the next orbit.
        /// </summary>
        /// <param name="target">Target Vessel.</param>
        [KRPCMethod]
        public double TimeOfClosestApproach(Vessel target)
        {
            double distance;
            return CalcClosestAproach(this, target.Orbit, Planetarium.GetUniversalTime(), out distance);
        }


        /// <summary>
        /// Estimates time of closest approach in the next orbit.
        /// </summary>
        /// <param name="target">Target vessel.</param>
        [KRPCMethod]
        public double DistanceAtClosestApproach(Vessel target)
        {

            double distance;
            CalcClosestAproach(this, target.Orbit, Planetarium.GetUniversalTime(), out distance);
            return distance;
        }


        /// <summary>
        /// Returns a list of two lists - the first of approach times, the second containing 
        /// the estimated distance for each of those approach times.
        /// </summary>
        /// <param name="target">Target Vessel.</param>
        /// <param name="orbits">Number of orbits to iterate through.</param>
        [KRPCMethod]
        public IList<IList<double>> ListClosestApproaches(Vessel target, int orbits)
        {
            IList<double> times = new List<double>();
            IList<double> distances = new List<double>();
            double distance;
            double orbitstart = Planetarium.GetUniversalTime();
            double period = InternalOrbit.period;
            for (int i = 0; i < orbits; i++)
            {
                times.Add(CalcClosestAproach(this, target.Orbit, orbitstart, out distance));
                distances.Add(distance);
                orbitstart += period;
            }
            IList<IList<double>> combined = new List<IList<double>>();
            combined.Add(times);
            combined.Add(distances);
            return combined;
        }

        /// <summary>
        ///  Helper function to calculate the closest approach to target in an orbital period.
        /// </summary>
        /// <param name="my_orbit">Orbit of the controlled vessel</param>
        /// <param name="target_orbit">Orbit of the target vessel</param>
        /// <param name="begin_time">Time to begin search - search continues for one orbital period</param>
        /// <param name="distance">Out parameter to return distance at the closest approach found</param>
        /// <returns></returns>
        public static double CalcClosestAproach(Orbit my_orbit, Orbit target_orbit, double begin_time, out double distance)
        {
             
            double approachTime = begin_time;
            double approachDistance = double.MaxValue;
            double mintime = begin_time;
            double interval = my_orbit.Period;
            if (my_orbit.Eccentricity > 1.0) { interval = 100 / my_orbit.InternalOrbit.meanMotion; }
            double maxtime = mintime + interval;

            //Conduct coarse search
            double timestep = (maxtime - mintime) / 20;
            double placeholder = mintime;
                    while (placeholder<maxtime)  
                    {
                        Vector3d PosA = my_orbit.InternalOrbit.getPositionAtUT(placeholder);
                        Vector3d PosB = target_orbit.InternalOrbit.getPositionAtUT(placeholder);
                        double thisDistance = Vector3d.Distance(PosA, PosB);
                        if (thisDistance<approachDistance)
                        {
                            approachDistance = thisDistance;
                            approachTime = placeholder;
                        }
                    placeholder += timestep;
                    }

                    //Conduct fine search
                    double fine_mintime = approachTime - timestep;
                    double fine_maxtime = approachTime + timestep;
                    if (fine_maxtime > maxtime) fine_maxtime = maxtime;
                    if (fine_mintime<mintime) fine_mintime = mintime;
                    timestep = (fine_maxtime - fine_mintime) / 50;
                    placeholder = fine_mintime;

                    while (placeholder<fine_maxtime)
                    {
                        Vector3d PosA = my_orbit.InternalOrbit.getPositionAtUT(placeholder);
                        Vector3d PosB = target_orbit.InternalOrbit.getPositionAtUT(placeholder);
                        double thisDistance = Vector3d.Distance(PosA, PosB);
                        if (thisDistance<approachDistance)
                        {
                            approachDistance = thisDistance;
                            approachTime = placeholder;
                        }
                        placeholder += timestep;
                    }
            distance = approachDistance;
            return approachTime;

        }


        public static double ClampRadiansTwoPi(double angle)
        {
            angle = angle % (2 * Math.PI);
            if (angle < 0) return angle + 2 * Math.PI;
            else return angle;
        }
    }
}
