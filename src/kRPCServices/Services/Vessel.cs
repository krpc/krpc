using System.Collections.Generic;
using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    /// <summary>
    /// Class representing a vessel. For example, can be used to control the vessel and get orbital data.
    /// </summary>
    [KRPCClass (Service = "Utils")]
    public class Vessel
    {
        VesselData vesselData;

        public Vessel (global::Vessel vessel)
        {
            vesselData = new VesselData (vessel);
            Orbit = new KRPCServices.Services.Orbit (vessel);
        }

        [KRPCProperty]
        public KRPCServices.Services.Orbit Orbit { get; private set; }

        [KRPCProperty]
        public string Body {
            get { return vesselData.Vessel.mainBody.name; }
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
        public double OrbitalSpeed {
            get { return vesselData.OrbitalSpeed; }
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

        [KRPCMethod]
        public double GetResource (string name)
        {
            // Get all resources
            var resources = new List<PartResource> ();
            var vessel = FlightGlobals.ActiveVessel;
            foreach (Part part in vessel.Parts) {
                foreach (PartResource resource in part.Resources) {
                    resources.Add (resource);
                }
            }
            return GetResourceAmount (name, resources);
        }

        double GetResourceAmount (string name, ICollection<PartResource> resources)
        {
            double amount = 0;
            foreach (var resource in resources) {
                if (resource.resourceName == name) {
                    amount += resource.amount;
                }
            }
            return amount;
        }
    }
}
