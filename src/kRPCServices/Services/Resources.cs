using System;
using KRPC.Service.Attributes;
using System.Collections.Generic;

namespace KRPCServices.Services
{
    /// <summary>
    /// Class used to manage the resources for a vessel.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Resources
    {
        //global::Vessel vessel;
        internal Resources (global::Vessel vessel)
        {
            //this.vessel = vessel;
        }
        //FIXME: what return type?
        //        [KRPCProperty]
        //        public string[] ResourceNames {
        //            get { throw new NotImplementedException (); }
        //        }
        [KRPCMethod]
        public bool HasResource (string name)
        {
            throw new NotImplementedException ();
        }

        [KRPCMethod]
        public double Max (string name, uint stage = 0)
        {
            throw new NotImplementedException ();
        }

        [KRPCMethod]
        public double Amount (string name, uint stage = 0)
        {
            throw new NotImplementedException ();
        }

        [KRPCMethod]
        public double Rate (string name)
        {
            throw new NotImplementedException ();
        }
        //[KRPCMethod]
        //public double GetResource (string name)
        //{
        //    // Get all resources
        //    var resources = new List<PartResource> ();
        //    foreach (Part part in vessel.Parts) {
        //        foreach (PartResource resource in part.Resources) {
        //            resources.Add (resource);
        //        }
        //    }
        //    return GetResourceAmount (name, resources);
        //}
        //
        //double GetResourceAmount (string name, ICollection<PartResource> resources)
        //{
        //    double amount = 0;
        //    foreach (var resource in resources) {
        //        if (resource.resourceName == name) {
        //            amount += resource.amount;
        //        }
        //    }
        //    return amount;
        //}
    }
}
