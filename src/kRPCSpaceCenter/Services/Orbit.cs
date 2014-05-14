using System;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Orbit
    {
        global::Orbit orbit;

        internal Orbit (global::Vessel vessel)
        {
            this.orbit = vessel.GetOrbit ();
        }

        internal Orbit (global::CelestialBody body)
        {
            if (body.name == "Sun")
                throw new ArgumentException ("The sun has no orbit");
            this.orbit = body.GetOrbit ();
        }

        [KRPCProperty]
        public CelestialBody Body {
            get { return SpaceCenter.Bodies [orbit.referenceBody.name]; }
        }

        [KRPCProperty]
        public double Apoapsis {
            get { return orbit.ApR; }
        }

        [KRPCProperty]
        public double Periapsis {
            get { return orbit.PeR; }
        }

        [KRPCProperty]
        public double ApoapsisAltitude {
            get { return orbit.ApA; }
        }

        [KRPCProperty]
        public double PeriapsisAltitude {
            get { return orbit.PeA; }
        }

        [KRPCProperty]
        public double SemiMajorAxis {
            get { return 0.5d * (Apoapsis + Periapsis); }
        }

        [KRPCProperty]
        public double SemiMajorAxisAltitude {
            get { return 0.5d * (ApoapsisAltitude + PeriapsisAltitude); }
        }

        [KRPCProperty]
        public double TimeToApoapsis {
            get { return orbit.timeToAp; }
        }

        [KRPCProperty]
        public double TimeToPeriapsis {
            get { return orbit.timeToPe; }
        }

        [KRPCProperty]
        public double TimeToSOIChange {
            get { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public double Eccentricity {
            get { return orbit.eccentricity; }
        }

        [KRPCProperty]
        public double Inclination {
            get { return orbit.inclination; }
        }

        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return orbit.LAN; }
        }

        [KRPCProperty]
        public double ArgumentOfPeriapsis {
            get { return orbit.argumentOfPeriapsis; }
        }

        [KRPCProperty]
        public double MeanAnomalyAtEpoch {
            get { return orbit.meanAnomalyAtEpoch; }
        }

        [KRPCProperty]
        public Orbit NextOrbit {
            get { throw new NotImplementedException (); }
        }
    }
}
