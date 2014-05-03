using System;
using KRPC.Service.Attributes;

namespace KRPCSpaceCenter.Services
{
    /// <summary>
    /// Class used to represent a celestial body, such as Kerbin or the Mun.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CelestialBody
    {
        global::CelestialBody body;
        Orbit orbit;

        public CelestialBody (global::CelestialBody body)
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
    }
}
