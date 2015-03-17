using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class CelestialBody : Equatable<CelestialBody>
    {
        Orbit orbit;

        internal CelestialBody (global::CelestialBody body)
        {
            InternalBody = body;
            // TODO: better way to check for orbits?
            if (body.name != "Sun")
                orbit = new Orbit (body);
        }

        internal global::CelestialBody InternalBody { get; private set; }

        public override bool Equals (CelestialBody obj)
        {
            return InternalBody == obj.InternalBody;
        }

        public override int GetHashCode ()
        {
            return InternalBody.GetHashCode ();
        }

        [KRPCProperty]
        public string Name {
            get { return InternalBody.name; }
        }

        [KRPCProperty]
        public IList<CelestialBody> Satellites {
            get {
                var allBodies = SpaceCenter.Bodies;
                var bodies = new List<CelestialBody> ();
                foreach (var body in InternalBody.orbitingBodies) {
                    bodies.Add (allBodies [body.name]);
                }
                return bodies;
            }
        }

        [KRPCProperty]
        public double Mass {
            get { return InternalBody.Mass; }
        }

        [KRPCProperty]
        public double GravitationalParameter {
            get { return InternalBody.gravParameter; }
        }

        [KRPCProperty]
        public double SurfaceGravity {
            get { return InternalBody.GeeASL * 9.81d; }
        }

        [KRPCProperty]
        public double RotationalPeriod {
            get { return InternalBody.rotationPeriod; }
        }

        [KRPCProperty]
        public double RotationalSpeed {
            get { return (2d * Math.PI) / RotationalPeriod; }
        }

        [KRPCProperty]
        public double EquatorialRadius {
            get { return InternalBody.Radius; }
        }

        [KRPCProperty]
        public double SphereOfInfluence {
            get { return InternalBody.sphereOfInfluence; }
        }

        [KRPCProperty]
        public Orbit Orbit {
            get { return orbit; }
        }

        [KRPCProperty]
        public bool HasAtmosphere {
            get { return InternalBody.atmosphere; }
        }

        [KRPCProperty]
        public double AtmospherePressure {
            get { return HasAtmosphere ? InternalBody.atmosphereMultiplier * 101325d : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereDensity {
            get { return HasAtmosphere ? InternalBody.atmosphereMultiplier * FlightGlobals.getAtmDensity (1d) : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereScaleHeight {
            get { return HasAtmosphere ? InternalBody.atmosphereScaleHeight * 1000d : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereMaxAltitude {
            get { return HasAtmosphere ? InternalBody.maxAtmosphereAltitude : 0d; }
        }

        [KRPCMethod]
        public double AtmospherePressureAt (double altitude)
        {
            return HasAtmosphere ? AtmospherePressure * Math.Exp (-altitude / AtmosphereScaleHeight) : 0d;
        }

        [KRPCMethod]
        public double AtmosphereDensityAt (double altitude)
        {
            return HasAtmosphere ? AtmosphereDensity * Math.Exp (-altitude / AtmosphereScaleHeight) : 0d;
        }

        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalBody); }
        }

        [KRPCProperty]
        public ReferenceFrame OrbitalReferenceFrame {
            get { return ReferenceFrame.Orbital (InternalBody); }
        }

        [KRPCMethod]
        public Tuple3 Position (ReferenceFrame referenceFrame)
        {
            return referenceFrame.PositionFromWorldSpace (InternalBody.position).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Velocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.VelocityFromWorldSpace (InternalBody.position, InternalBody.GetWorldVelocity ()).ToTuple ();
        }

        [KRPCMethod]
        public Tuple4 Rotation (ReferenceFrame referenceFrame)
        {
            var up = Vector3.up;
            var right = InternalBody.GetRelSurfacePosition (0, 0, 1).normalized;
            var forward = Vector3.Cross (right, up);
            Vector3.OrthoNormalize (ref forward, ref up);
            var rotation = Quaternion.LookRotation (forward, up);
            return referenceFrame.RotationFromWorldSpace (rotation).ToTuple ();
        }

        [KRPCMethod]
        public Tuple3 Direction (ReferenceFrame referenceFrame)
        {
            return referenceFrame.DirectionFromWorldSpace (InternalBody.transform.up).ToTuple ();
        }

        //TODO: default argument value?
        [KRPCMethod]
        public Tuple3 AngularVelocity (ReferenceFrame referenceFrame)
        {
            return referenceFrame.AngularVelocityFromWorldSpace (InternalBody.angularVelocity).ToTuple ();
        }
    }
}
