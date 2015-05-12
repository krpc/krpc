using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Resources : Equatable<Resources>
    {
        readonly global::Vessel vessel;
        readonly int stage;
        readonly bool cumulative;
        readonly IList<Part> parts;

        internal Resources (global::Vessel vessel, int stage = -1, bool cumulative = true)
        {
            this.vessel = vessel;
            this.stage = stage;
            this.cumulative = cumulative;
            parts = new List<Part> ();
        }

        internal Resources (Part part)
        {
            vessel = null;
            stage = -1;
            cumulative = true;
            parts = new List<Part> ();
            parts.Add (part);
        }

        internal Resources (IList<Part> parts)
        {
            vessel = null;
            stage = -1;
            cumulative = true;
            this.parts = parts;
        }

        public override bool Equals (Resources obj)
        {
            return vessel == obj.vessel && stage == obj.stage && cumulative == obj.cumulative && parts.SequenceEqual (obj.parts);
        }

        public override int GetHashCode ()
        {
            return vessel.GetHashCode () ^ stage.GetHashCode () ^ cumulative.GetHashCode () ^ parts.GetHashCode ();
        }

        List<PartResource> GetResources ()
        {
            var resources = new List<PartResource> ();
            if (vessel != null) {
                foreach (var part in vessel.Parts) {
                    if (stage < 0 || part.DecoupledAt () + 1 == stage || (cumulative && part.DecoupledAt () < stage)) {
                        foreach (PartResource resource in part.Resources)
                            resources.Add (resource);
                    }
                }
            } else {
                foreach (var part in parts)
                    foreach (PartResource resource in part.Resources)
                        resources.Add (resource);
            }
            return resources;
        }

        [KRPCProperty]
        public IList<string> Names {
            get {
                return GetResources ().Select (r => r.resourceName).Distinct ().ToList ();
            }
        }

        [KRPCMethod]
        public bool HasResource (string name)
        {
            return GetResources ().Any (r => r.resourceName == name);
        }

        [KRPCMethod]
        public float Max (string name)
        {
            return GetResources ().Where (r => r.resourceName == name).Sum (r => (float)r.maxAmount);
        }

        [KRPCMethod]
        public float Amount (string name)
        {
            return GetResources ().Where (r => r.resourceName == name).Sum (r => (float)r.amount);
        }

        [KRPCMethod]
        public static float Density (string name)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition (name);
            if (resource == null)
                throw new ArgumentException ("Resource not found");
            return resource.density;
        }
    }
}
