using System;
using KRPC.Service.Attributes;
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
        public KRPC.Schema.Geometry.Vector3 Velocity {
            get { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 Speed {
            get { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public double HorizontalSpeed {
            get { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public double VerticalSpeed {
            get { throw new NotImplementedException (); }
        }

        [KRPCProperty]
        public KRPC.Schema.Geometry.Vector3 CenterOfMass {
            get { return Utils.ToVector3 (vesselData.Position); }
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
    }
}
