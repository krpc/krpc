using KRPC.Service.Attributes;
using KRPCServices.ExtensionMethods;

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
        /// Prograde direction in surface reference frame
        /// </summary>
        Vector3d GetPrograde ()
        {
            var rot = ReferenceFrameRotation.Get (ReferenceFrame.Surface, vessel).Inverse ();
            return (rot * vessel.GetOrbit ().GetVel ()).normalized;
        }

        /// <summary>
        /// Normal+ direction in surface reference frame
        /// </summary>
        Vector3d GetNormal ()
        {
            var rot = ReferenceFrameRotation.Get (ReferenceFrame.Surface, vessel).Inverse ();
            var normal = vessel.GetOrbit ().GetOrbitNormal ();
            var tmp = normal.y;
            normal.y = normal.z;
            normal.z = tmp;
            return (rot * normal).normalized;
        }

        /// <summary>
        /// Radial+ direction in surface reference frame
        /// </summary>
        Vector3d GetRadial ()
        {
            return Vector3d.Cross (GetNormal (), GetPrograde ()).normalized;
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Prograde {
            get { return GetPrograde ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Retrograde {
            get { return (-GetPrograde ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Normal {
            get { return GetNormal ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NormalNeg {
            get { return (-GetNormal ()).ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Radial {
            get { return GetRadial ().ToMessage (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 RadialNeg {
            get { return (-GetRadial ()).ToMessage (); }
        }
    }
}
