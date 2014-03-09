using KRPC.Service.Attributes;
using UnityEngine;
using KRPC.Schema.Geometry;

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
            get { return VesselData.SurfaceSpeed; }
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
        public static KRPC.Schema.Geometry.Vector3 Direction {
            get { return Utils.ToVector3 (VesselData.Direction); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 UpDirection {
            get { return Utils.ToVector3 (VesselData.UpDirection); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 NorthDirection {
            get { return Utils.ToVector3 (VesselData.NorthDirection); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 CenterOfMass {
            get { return Utils.ToVector3 (VesselData.Position); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 Prograde {
            get { return Utils.ToVector3 (VesselData.Prograde); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 Retrograde {
            get { return Utils.ToVector3 (VesselData.Retrograde); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 Normal {
            get { return Utils.ToVector3 (VesselData.Normal); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 NormalNeg {
            get { return Utils.ToVector3 (VesselData.NormalNeg); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 Radial {
            get { return Utils.ToVector3 (VesselData.Radial); }
        }

        [KRPCProperty]
        public static KRPC.Schema.Geometry.Vector3 RadialNeg {
            get { return Utils.ToVector3 (VesselData.RadialNeg); }
        }
    }
}
