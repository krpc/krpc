using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceFlowMode
    {
        Vessel,
        Stage,
        Adjacent,
        None
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Resources : Equatable<Resources>
    {
        readonly global::Vessel vessel;
        readonly int stage;
        readonly bool cumulative;
        readonly Part part;

        internal Resources (global::Vessel vessel, int stage = -1, bool cumulative = true)
        {
            this.vessel = vessel;
            this.stage = stage;
            this.cumulative = cumulative;
            part = null;
        }

        internal Resources (Part part)
        {
            vessel = null;
            stage = -1;
            cumulative = true;
            this.part = part;
        }

        public override bool Equals (Resources obj)
        {
            return vessel == obj.vessel && stage == obj.stage && cumulative == obj.cumulative && part == obj.part;
        }

        public override int GetHashCode ()
        {
            return (vessel == null ? 0 : vessel.GetHashCode ()) ^ stage.GetHashCode () ^ cumulative.GetHashCode () ^ (part == null ? 0 : part.GetHashCode ());
        }

        List<PartResource> GetResources ()
        {
            var resources = new List<PartResource> ();
            if (vessel != null) {
                foreach (var vesselPart in vessel.Parts) {
                    if (stage < 0 || vesselPart.DecoupledAt () + 1 == stage || (cumulative && vesselPart.DecoupledAt () < stage)) {
                        foreach (PartResource resource in vesselPart.Resources)
                            resources.Add (resource);
                    }
                }
            }
            if (part != null) {
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
            return resource.density * 1000f;
        }

        [KRPCMethod]
        public static ResourceFlowMode FlowMode (string name)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition (name);
            if (resource == null)
                throw new ArgumentException ("Resource not found");
            return resource.resourceFlowMode.ToResourceFlowMode ();
        }
    }
}
