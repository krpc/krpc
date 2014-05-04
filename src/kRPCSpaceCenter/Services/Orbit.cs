using System;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Orbit
    {
        global::Vessel vessel;
        global::Orbit orbit;

        internal Orbit (global::Vessel vessel)
        {
            this.vessel = vessel;
            this.orbit = vessel.GetOrbit ();
        }

        internal Orbit (global::CelestialBody body)
        {
            if (body.name == "Sun")
                throw new ArgumentException ("The sun has no orbit");
            //FIXME: vessel is null
            this.orbit = body.GetOrbit ();
        }

        [KRPCProperty]
        public CelestialBody Body {
            get { return SpaceCenter.Body (orbit.referenceBody.name); }
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

        /// <summary>
        /// Prograde direction in surface reference frame
        /// </summary>
        Vector3d GetPrograde ()
        {
            var rot = ReferenceFrameTransform.GetRotation (ReferenceFrame.Surface, vessel).Inverse ();
            return (rot * orbit.GetVel ()).normalized;
        }

        /// <summary>
        /// Normal+ direction in surface reference frame
        /// </summary>
        Vector3d GetNormal ()
        {
            var rot = ReferenceFrameTransform.GetRotation (ReferenceFrame.Surface, vessel).Inverse ();
            var normal = orbit.GetOrbitNormal ();
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
