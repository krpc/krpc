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
        public float Mass {
            get { return (float)InternalBody.Mass; }
        }

        [KRPCProperty]
        public float GravitationalParameter {
            get { return (float)InternalBody.gravParameter; }
        }

        [KRPCProperty]
        public float SurfaceGravity {
            get { return (float)InternalBody.GeeASL * 9.81f; }
        }

        [KRPCProperty]
        public float RotationalPeriod {
            get { return (float)InternalBody.rotationPeriod; }
        }

        [KRPCProperty]
        public float RotationalSpeed {
            get { return (float)(2f * Math.PI) / RotationalPeriod; }
        }

        [KRPCProperty]
        public float EquatorialRadius {
            get { return (float)InternalBody.Radius; }
        }

        [KRPCProperty]
        public float SphereOfInfluence {
            get { return (float)InternalBody.sphereOfInfluence; }
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
        public float AtmosphereDepth {
            get { return (float)InternalBody.atmosphereDepth; }
        }

        [KRPCProperty]
        public bool HasAtmosphericOxygen {
            get { return InternalBody.atmosphereContainsOxygen; }
        }

        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return ReferenceFrame.Object (InternalBody); }
        }

        [KRPCProperty]
        public ReferenceFrame NonRotatingReferenceFrame {
            get { return ReferenceFrame.NonRotating (InternalBody); }
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
