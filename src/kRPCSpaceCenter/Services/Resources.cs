using System;
using System.Linq;
using KRPC.Service.Attributes;
using KRPCSpaceCenter.ExtensionMethods;
using System.Collections.Generic;

namespace KRPCSpaceCenter.Services
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Resources
    {
        global::Vessel vessel;

        internal Resources (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        List<PartResource> GetResources (int stage = -1, bool cumulative = false)
        {
            var resources = new List<PartResource> ();
            foreach (var part in vessel.Parts) {
                if (stage < 0 || part.DecoupledAt () + 1 == stage || (cumulative && part.DecoupledAt () < stage)) {
                    foreach (PartResource resource in part.Resources)
                        resources.Add (resource);
                }
            }
            return resources;
        }

        [KRPCProperty]
        public IList<string> Names {
            get {
                var names = new HashSet<string> ();
                foreach (var part in vessel.Parts) {
                    foreach (PartResource resource in part.Resources)
                        names.Add (resource.resourceName);
                }
                return names.ToList ();
            }
        }

        [KRPCMethod]
        public bool HasResource (string name)
        {
            foreach (var resource in GetResources())
                if (resource.resourceName == name)
                    return true;
            return false;
        }

        [KRPCMethod]
        public double Max (string name, int stage = -1, bool cumulative = true)
        {
            double amount = 0;
            foreach (var resource in GetResources(stage, cumulative)) {
                if (resource.resourceName == name)
                    amount += resource.maxAmount;
            }
            return amount;
        }

        [KRPCMethod]
        public double Amount (string name, int stage = -1, bool cumulative = true)
        {
            double amount = 0;
            foreach (var resource in GetResources(stage, cumulative)) {
                if (resource.resourceName == name)
                    amount += resource.amount;
            }
            return amount;
        }
    }
}
