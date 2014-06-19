using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class CelestialBody : Equatable<CelestialBody>
    {
        Orbit orbit;

        internal CelestialBody (global::CelestialBody body)
        {
            Body = body;
            if (body.name != "Sun")
                this.orbit = new Orbit (body);
        }

        internal global::CelestialBody Body { get; private set; }

        public override bool Equals (CelestialBody other)
        {
            return Body == other.Body;
        }

        public override int GetHashCode ()
        {
            return Body.GetHashCode ();
        }

        [KRPCProperty]
        public string Name {
            get { return Body.name; }
        }

        [KRPCProperty]
        public IList<CelestialBody> Satellites {
            get {
                var allBodies = SpaceCenter.Bodies;
                var bodies = new List<CelestialBody> ();
                foreach (var body in Body.orbitingBodies) {
                    bodies.Add (allBodies [body.name]);
                }
                return bodies;
            }
        }

        [KRPCProperty]
        public double Mass {
            get { return Body.Mass; }
        }

        [KRPCProperty]
        public double GravitationalParameter {
            get { return Body.gravParameter; }
        }

        [KRPCProperty]
        public double SurfaceGravity {
            get { return Body.GeeASL * 9.81d; }
        }

        [KRPCProperty]
        public double RotationalPeriod {
            get { return Body.rotationPeriod; }
        }

        [KRPCProperty]
        public double EquatorialRadius {
            get { return Body.Radius; }
        }

        [KRPCProperty]
        public double SphereOfInfluence {
            get { return Body.sphereOfInfluence; }
        }

        [KRPCProperty]
        public Orbit Orbit {
            get { return orbit; }
        }

        [KRPCProperty]
        public bool HasAtmosphere {
            get { return Body.atmosphere; }
        }

        [KRPCProperty]
        public double AtmospherePressure {
            get { return HasAtmosphere ? Body.atmosphereMultiplier * 101325d : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereDensity {
            get { return HasAtmosphere ? Body.atmosphereMultiplier * FlightGlobals.getAtmDensity (1d) : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereScaleHeight {
            get { return HasAtmosphere ? Body.atmosphereScaleHeight * 1000 : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereMaxAltitude {
            get { return HasAtmosphere ? Body.maxAtmosphereAltitude : 0d; }
        }
    }
}
