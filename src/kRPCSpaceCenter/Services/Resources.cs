using System;
using KRPC.Service.Attributes;
using System.Collections.Generic;

namespace KRPCSpaceCenter.Services
{
    /// <summary>
    /// Class used to manage the resources for a vessel.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Resources
    {
        global::Vessel vessel;

        internal Resources (global::Vessel vessel)
        {
            this.vessel = vessel;
        }

        List<PartResource> GetResources (uint stage = 0)
        {
            var resources = new List<PartResource> ();
            foreach (Part part in vessel.Parts) {
                if (stage == 0 || part.inverseStage == stage) {
                    foreach (PartResource resource in part.Resources)
                        resources.Add (resource);
                }
            }
            return resources;
        }
        //FIXME: what return type?
        //[KRPCProperty]
        //public string[] ResourceNames {
        //    get { throw new NotImplementedException (); }
        //}
        [KRPCMethod]
        public bool HasResource (string name)
        {
            foreach (var resource in GetResources())
                if (resource.resourceName == name)
                    return true;
            return false;
        }

        [KRPCMethod]
        public double Max (string name, uint stage = 0)
        {
            double amount = 0;
            foreach (var resource in GetResources(stage)) {
                if (resource.resourceName == name)
                    amount += resource.maxAmount;
            }
            return amount;
        }

        [KRPCMethod]
        public double Amount (string name, uint stage = 0)
        {
            double amount = 0;
            foreach (var resource in GetResources(stage)) {
                if (resource.resourceName == name)
                    amount += resource.amount;
            }
            return amount;
        }

        [KRPCMethod]
        public double Rate (string name)
        {
            throw new NotImplementedException ();
        }
    }
}
