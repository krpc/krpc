using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    /// <summary>
    /// Represents an orbit of an object.
    /// </summary>
    // TODO: only current supports orbits of vessels. Extend to include CelestialBodies
    [KRPCClass (Service = "Utils")]
    public class Orbit
    {
        global::Vessel vessel;

        public Orbit (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        [KRPCProperty]
        public string Body {
            get { return vessel.GetOrbit ().referenceBody.name; }
        }

        [KRPCProperty]
        public double Apoapsis {
            get { return vessel.GetOrbit ().ApR; }
        }

        [KRPCProperty]
        public double Periapsis {
            get { return vessel.GetOrbit ().PeR; }
        }

        [KRPCProperty]
        public double ApoapsisAltitude {
            get { return vessel.GetOrbit ().ApA; }
        }

        [KRPCProperty]
        public double PeriapsisAltitude {
            get { return vessel.GetOrbit ().PeA; }
        }

        [KRPCProperty]
        public double TimeToApoapsis {
            get { return vessel.GetOrbit ().timeToAp; }
        }

        [KRPCProperty]
        public double TimeToPeriapsis {
            get { return vessel.GetOrbit ().timeToPe; }
        }

        [KRPCProperty]
        public double Eccentricity {
            get { return vessel.GetOrbit ().eccentricity; }
        }

        [KRPCProperty]
        public double Inclination {
            get { return vessel.GetOrbit ().inclination; }
        }

        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return vessel.GetOrbit ().LAN; }
        }

        [KRPCProperty]
        public double ArgumentOfPeriapsis {
            get { return vessel.GetOrbit ().argumentOfPeriapsis; }
        }

        [KRPCProperty]
        public double MeanAnomalyAtEpoch {
            get { return vessel.GetOrbit ().meanAnomalyAtEpoch; }
        }
    }
}
