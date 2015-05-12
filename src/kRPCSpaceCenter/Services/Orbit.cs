using System;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;

namespace KRPCSpaceCenter.Services
{
    //FIXME: should extend equatable interface?
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Orbit
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

        public Orbit (global::Orbit orbit)
        {
            InternalOrbit = orbit;
        }

        public global::Orbit InternalOrbit { get; private set; }

        //TODO: make equatable? add hashcode???

        [KRPCProperty]
        public CelestialBody Body {
            get { return SpaceCenter.Bodies [InternalOrbit.referenceBody.name]; }
        }

        [KRPCProperty]
        public double Apoapsis {
            get { return InternalOrbit.ApR; }
        }

        [KRPCProperty]
        public double Periapsis {
            get { return InternalOrbit.PeR; }
        }

        [KRPCProperty]
        public double ApoapsisAltitude {
            get { return InternalOrbit.ApA; }
        }

        [KRPCProperty]
        public double PeriapsisAltitude {
            get { return InternalOrbit.PeA; }
        }

        [KRPCProperty]
        public double SemiMajorAxis {
            get { return 0.5d * (Apoapsis + Periapsis); }
        }

        [KRPCProperty]
        public double SemiMinorAxis {
            get { return SemiMajorAxis * Math.Sqrt (1d - (Eccentricity * Eccentricity)); }
        }

        [KRPCProperty]
        public double Radius {
            get { return InternalOrbit.radius; }
        }

        [KRPCProperty]
        public double Speed {
            get { return InternalOrbit.orbitalSpeed; }
        }

        [KRPCProperty]
        public double Period {
            get { return InternalOrbit.period; }
        }

        [KRPCProperty]
        public double TimeToApoapsis {
            get { return InternalOrbit.timeToAp; }
        }

        [KRPCProperty]
        public double TimeToPeriapsis {
            get { return InternalOrbit.timeToPe; }
        }

        [KRPCProperty]
        public double Eccentricity {
            get { return InternalOrbit.eccentricity; }
        }

        [KRPCProperty]
        public double Inclination {
            get { return InternalOrbit.inclination * (Math.PI / 180d); }
        }

        [KRPCProperty]
        public double LongitudeOfAscendingNode {
            get { return InternalOrbit.LAN * (Math.PI / 180d); }
        }

        [KRPCProperty]
        public double ArgumentOfPeriapsis {
            get { return InternalOrbit.argumentOfPeriapsis * (Math.PI / 180d); }
        }

        [KRPCProperty]
        public double MeanAnomalyAtEpoch {
            get { return InternalOrbit.meanAnomalyAtEpoch; }
        }

        [KRPCProperty]
        public double Epoch {
            get { return InternalOrbit.epoch; }
        }

        [KRPCProperty]
        public double MeanAnomaly {
            get { return InternalOrbit.meanAnomaly; }
        }

        [KRPCProperty]
        public double EccentricAnomaly {
            get { return InternalOrbit.eccentricAnomaly; }
        }

        [KRPCMethod]
        public static Tuple3 ReferencePlaneNormal (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (Planetarium.up).normalized.ToTuple ();
        }

        [KRPCMethod]
        public static Tuple3 ReferencePlaneDirection (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (Planetarium.right).normalized.ToTuple ();
        }

        [KRPCProperty]
        public Orbit NextOrbit {
            get {
                return (Double.IsNaN (TimeToSOIChange)) ? null : new Orbit (InternalOrbit.nextPatch);
            }
        }

        [KRPCProperty]
        public double TimeToSOIChange {
            get {
                var time = InternalOrbit.UTsoi - SpaceCenter.UT;
                return time < 0 ? Double.NaN : time;
            }
        }
    }
}
