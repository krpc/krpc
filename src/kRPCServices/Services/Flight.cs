using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPCServices.Services
{
    [KRPCService]
    public static class Flight
    {
        internal static VesselData VesselData { set; get; }

        [KRPCProperty]
        public static string Body {
            get { return VesselData.Vessel.mainBody.name; }
        }

        [KRPCProperty]
        public static double Altitude {
            get { return VesselData.Altitude; }
        }

        [KRPCProperty]
        public static double TrueAltitude {
            get { return VesselData.TrueAltitude; }
        }

        [KRPCProperty]
        public static double OrbitalSpeed {
            get { return VesselData.OrbitalSpeed; }
        }

        [KRPCProperty]
        public static double SurfaceSpeed {
            get { return VesselData.OrbitalSpeed; }
        }

        [KRPCProperty]
        public static double VerticalSurfaceSpeed {
            get { return VesselData.VerticalSurfaceSpeed; }
        }

        [KRPCProperty]
        public static double Pitch {
            get { return VesselData.Pitch; }
        }

        [KRPCProperty]
        public static double Heading {
            get { return VesselData.Heading; }
        }

        [KRPCProperty]
        public static double Roll {
            get { return VesselData.Roll; }
        }

        [KRPCProperty]
        public static Vector3 Direction {
            get { return VesselData.Direction; }
        }

        [KRPCProperty]
        public static Vector3 UpDirection {
            get { return VesselData.UpDirection; }
        }

        [KRPCProperty]
        public static Vector3 CenterOfMass {
            get { return VesselData.Position; }
        }

        [KRPCProperty]
        public static Vector3 Prograde {
            get { return VesselData.Prograde; }
        }

        [KRPCProperty]
        public static Vector3 Retrograde {
            get { return VesselData.Retrograde; }
        }

        [KRPCProperty]
        public static Vector3 Normal {
            get { return VesselData.Normal; }
        }

        [KRPCProperty]
        public static Vector3 NormalNeg {
            get { return VesselData.NormalNeg; }
        }

        [KRPCProperty]
        public static Vector3 Radial {
            get { return VesselData.Radial; }
        }

        [KRPCProperty]
        public static Vector3 RadialNeg {
            get { return VesselData.RadialNeg; }
        }
    }
}
