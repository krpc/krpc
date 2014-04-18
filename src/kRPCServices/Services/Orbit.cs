using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public class Orbit
    {
        global::Vessel vessel;

        internal Orbit (global::Vessel vessel)
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

        /// <summary>
        /// Prograde direction in world coordinates
        /// </summary>
        Vector3d GetPrograde ()
        {
            return vessel.GetOrbit ().GetVel ().normalized;
        }

        /// <summary>
        /// Normal+ direction in world coordinates
        /// </summary>
        Vector3d GetNormal ()
        {
            return Vector3d.Cross (GetPrograde (), GetRadial ());
        }

        /// <summary>
        /// Radial+ direction in world coordinates
        /// </summary>
        Vector3d GetRadial ()
        {
            var orbitalVelocity = vessel.GetOrbit ().GetVel ();
            var upDirection = (vessel.findWorldCenterOfMass () - vessel.mainBody.position).normalized;
            return Vector3d.Exclude (orbitalVelocity, upDirection).normalized; // TODO: does this have to be normalized?
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Prograde {
            get { return Utils.ToVector3 (GetPrograde ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Retrograde {
            get { return Utils.ToVector3 (-GetPrograde ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Normal {
            get { return Utils.ToVector3 (GetNormal ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NormalNeg {
            get { return Utils.ToVector3 (-GetNormal ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Radial {
            get { return Utils.ToVector3 (GetRadial ()); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 RadialNeg {
            get { return Utils.ToVector3 (-GetRadial ()); }
        }
    }
}
