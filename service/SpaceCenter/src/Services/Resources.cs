using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Created by calling <see cref="Vessel.Resources"/>,
    /// <see cref="Vessel.ResourcesInDecoupleStage"/> or
    /// <see cref="Parts.Part.Resources"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Resources : Equatable<Resources>
    {
        readonly Guid vesselId;
        readonly int stage;
        readonly bool cumulative;
        readonly uint partId;

        internal Resources (global::Vessel vessel, int stage = -1, bool cumulative = true)
        {
            vesselId = vessel.id;
            this.stage = stage;
            this.cumulative = cumulative;
            partId = 0;
        }

        internal Resources (Part part)
        {
            vesselId = Guid.Empty;
            stage = -1;
            cumulative = true;
            partId = part.flightID;
        }

        /// <summary>
        /// Check if resources objects are equal.
        /// </summary>
        public override bool Equals (Resources obj)
        {
            return vesselId == obj.vesselId && stage == obj.stage && cumulative == obj.cumulative && partId == obj.partId;
        }

        /// <summary>
        /// Hash the resources object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode () ^ stage.GetHashCode () ^ cumulative.GetHashCode () ^ partId.GetHashCode ();
        }

        /// <summary>
        /// The KSP vessel.
        /// </summary>
        public global::Vessel InternalVessel {
            get {
                if (vesselId == Guid.Empty)
                    throw new InvalidOperationException ("Resources object has no vessel");
                return FlightGlobalsExtensions.GetVesselById (vesselId);
            }
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public Part InternalPart {
            get {
                if (partId == 0)
                    throw new InvalidOperationException ("Resources object has no part");
                return FlightGlobals.FindPartByID (partId);
            }
        }

        List<PartResource> GetResources ()
        {
            var resources = new List<PartResource> ();
            if (vesselId != Guid.Empty) {
                foreach (var vesselPart in InternalVessel.Parts) {
                    if (stage < 0 || vesselPart.DecoupledAt () + 1 == stage || (cumulative && vesselPart.DecoupledAt () < stage)) {
                        foreach (PartResource resource in vesselPart.Resources)
                            resources.Add (resource);
                    }
                }
            }
            if (partId != 0) {
                foreach (PartResource resource in InternalPart.Resources)
                    resources.Add (resource);
            }
            return resources;
        }

        /// <summary>
        /// All the individual resources that can be stored.
        /// </summary>
        [KRPCProperty]
        public IList<Resource> All {
            get { return GetResources ().Select (r => new Resource (r)).ToList (); }
        }

        /// <summary>
        /// All the individual resources with the given name that can be stored.
        /// </summary>
        [KRPCMethod]
        public IList<Resource> WithResource (string name)
        {
            return GetResources ().Where (r => r.resourceName == name).Select (r => new Resource (r)).ToList ();
        }

        /// <summary>
        /// A list of resource names that can be stored.
        /// </summary>
        [KRPCProperty]
        public IList<string> Names {
            get {
                return GetResources ().Select (r => r.resourceName).Distinct ().ToList ();
            }
        }

        /// <summary>
        /// Check whether the named resource can be stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public bool HasResource (string name)
        {
            return GetResources ().Any (r => r.resourceName == name);
        }

        /// <summary>
        /// Returns the amount of a resource that can be stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public float Max (string name)
        {
            return GetResources ().Where (r => r.resourceName == name).Sum (r => (float)r.maxAmount);
        }

        /// <summary>
        /// Returns the amount of a resource that is currently stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public float Amount (string name)
        {
            return GetResources ().Where (r => r.resourceName == name).Sum (r => (float)r.amount);
        }

        /// <summary>
        /// Returns the density of a resource, in kg/l.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public static float Density (string name)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition (name);
            if (resource == null)
                throw new ArgumentException ("Resource not found");
            return resource.density * 1000f;
        }

        /// <summary>
        /// Returns the flow mode of a resource.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
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
