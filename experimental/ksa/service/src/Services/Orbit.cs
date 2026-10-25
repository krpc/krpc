using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The orbit of a vehicle, obtained from <see cref="Vehicle.Orbit"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Orbit : Equatable<Orbit>
    {
        readonly global::KSA.Vehicle vehicle;

        internal Orbit (global::KSA.Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Orbit other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (vehicle, other.vehicle);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        // The vehicle's current orbit. The vehicle replaces the object as its trajectory changes
        global::KSA.Orbit InternalOrbit {
            get { return new Vehicle (vehicle).InternalVehicle.Orbit; }
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
        /// The semi-major axis of the orbit, in meters.
        /// </summary>
        [KRPCProperty]
        public double SemiMajorAxis {
            get { return InternalOrbit.SemiMajorAxis; }
        }

        /// <summary>
        /// The orbital speed of the vehicle, in meters per second.
        /// </summary>
        [KRPCProperty]
        public double Speed {
            get { return new Vehicle (vehicle).InternalVehicle.OrbitalSpeed; }
        }

        /// <summary>
        /// The orbital period, in seconds. Not a number for a parabolic or hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double Period {
            get { return InternalOrbit.Period; }
        }

        /// <summary>
        /// The time until the vehicle reaches apoapsis, in seconds. Not a number for a
        /// parabolic or hyperbolic orbit.
        /// </summary>
        [KRPCProperty]
        public double TimeToApoapsis {
            get {
                var v = new Vehicle (vehicle).InternalVehicle;
                if (v.Orbit.Eccentricity >= 1)
                    return double.NaN;
                return (v.NextApoapsisTime - Universe.GetElapsedTime ()).Seconds ();
            }
        }

        /// <summary>
        /// The time until the vehicle reaches periapsis, in seconds. Negative once a
        /// vehicle on a hyperbolic orbit has passed it.
        /// </summary>
        [KRPCProperty]
        public double TimeToPeriapsis {
            get {
                var v = new Vehicle (vehicle).InternalVehicle;
                return (v.NextPeriapsisTime - Universe.GetElapsedTime ()).Seconds ();
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
    }
}
