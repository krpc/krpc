using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// See <see cref="Resources.FlowMode"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceFlowMode
    {
        /// <summary>
        /// The resource flows to any part in the vessel. For example, electric charge.
        /// </summary>
        Vessel,
        /// <summary>
        /// The resource flows from parts in the first stage, followed by the second,
        /// and so on. For example, mono-propellant.
        /// </summary>
        Stage,
        /// <summary>
        /// The resource flows between adjacent parts within the vessel. For example,
        /// liquid fuel or oxidizer.
        /// </summary>
        Adjacent,
        /// <summary>
        /// The resource does not flow. For example, solid fuel.
        /// </summary>
        None
    }

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
            this.vesselId = vessel.id;
            this.stage = stage;
            this.cumulative = cumulative;
            this.partId = 0;
        }

        internal Resources (global::Part part)
        {
            this.vesselId = Guid.Empty;
            this.stage = -1;
            this.cumulative = true;
            this.partId = part.flightID;
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
        public global::Part InternalPart {
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
