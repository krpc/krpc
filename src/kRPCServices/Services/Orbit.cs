using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    /// <summary>
    /// Represents an orbit of an object such as a vessel or a celestial body.
    /// </summary>
    // TODO: extend to include CelestialBodies
    [KRPCClass (Service = "SpaceCenter")]
    public class Orbit
    {
        global::Vessel vessel;
        VesselData vesselData;

        internal Orbit (VesselData vesselData)
        {
            this.vessel = vesselData.Vessel;
            this.vesselData = vesselData;
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

        [KRPCProperty]
        public double OrbitalSpeed {
            get { return vesselData.OrbitalSpeed; }
        }
    }
}
