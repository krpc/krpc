using KRPC.Service.Attributes;
using UnityEngine;
using KRPC.Schema.Geometry;

namespace KRPCServices.Services
{
    /// <summary>
    /// Information about the flight of a vessel. For orbital data, see SpaceCenter.Orbit
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Flight
    {
        VesselData vesselData;

        internal Flight (VesselData vesselData)
        {
            this.vesselData = vesselData;
        }

        [KRPCProperty]
        public double Altitude {
            get { return vesselData.Altitude; }
        }

        [KRPCProperty]
        public double TrueAltitude {
            get { return vesselData.TrueAltitude; }
        }

        [KRPCProperty]
        public double SurfaceSpeed {
            get { return vesselData.SurfaceSpeed; }
        }

        [KRPCProperty]
        public double VerticalSurfaceSpeed {
            get { return vesselData.VerticalSurfaceSpeed; }
        }

        [KRPCProperty]
        public double Pitch {
            get { return vesselData.Pitch; }
        }

        [KRPCProperty]
        public double Heading {
            get { return vesselData.Heading; }
        }

        [KRPCProperty]
        public double Roll {
            get { return vesselData.Roll; }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Direction {
            get { return Utils.ToVector3 (vesselData.Direction); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 UpDirection {
            get { return Utils.ToVector3 (vesselData.UpDirection); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NorthDirection {
            get { return Utils.ToVector3 (vesselData.NorthDirection); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 CenterOfMass {
            get { return Utils.ToVector3 (vesselData.Position); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Prograde {
            get { return Utils.ToVector3 (vesselData.Prograde); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Retrograde {
            get { return Utils.ToVector3 (vesselData.Retrograde); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Normal {
            get { return Utils.ToVector3 (vesselData.Normal); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 NormalNeg {
            get { return Utils.ToVector3 (vesselData.NormalNeg); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Radial {
            get { return Utils.ToVector3 (vesselData.Radial); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 RadialNeg {
            get { return Utils.ToVector3 (vesselData.RadialNeg); }
        }
    }
}
