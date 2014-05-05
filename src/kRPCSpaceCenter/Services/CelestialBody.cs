using System;
using KRPC.Service.Attributes;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class CelestialBody
    {
        // FIXME: make this private
        internal global::CelestialBody body;
        Orbit orbit;

        internal CelestialBody (global::CelestialBody body)
        {
            this.body = body;
            if (body.name != "Sun")
                this.orbit = new Orbit (body);
        }

        [KRPCProperty]
        public string Name {
            get { return body.name; }
        }

        [KRPCProperty]
        public double Mass {
            get { return body.Mass; }
        }

        [KRPCProperty]
        public double GravitationalParameter {
            get { return body.gravParameter; }
        }

        [KRPCProperty]
        public double SurfaceGravity {
            get { return body.GeeASL * 9.81d; }
        }

        [KRPCProperty]
        public double RotationalPeriod {
            get { return body.rotationPeriod; }
        }

        [KRPCProperty]
        public double EquatorialRadius {
            get { return body.Radius; }
        }

        [KRPCProperty]
        public double SphereOfInfluence {
            get { return body.sphereOfInfluence; }
        }

        [KRPCProperty]
        public Orbit Orbit {
            get { return orbit; }
        }

        [KRPCProperty]
        public bool HasAtmosphere {
            get { return body.atmosphere; }
        }

        [KRPCProperty]
        public double AtmospherePressure {
            get { return HasAtmosphere ? body.atmosphereMultiplier * 101.325d : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereScaleHeight {
            get { return HasAtmosphere ? body.atmosphereScaleHeight * 1000 : 0d; }
        }

        [KRPCProperty]
        public double AtmosphereMaxAltitude {
            get { return HasAtmosphere ? body.maxAtmosphereAltitude : 0d; }
        }
    }
}
