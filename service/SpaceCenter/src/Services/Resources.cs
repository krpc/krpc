using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// Represents the collection of resources stored in a vessel, stage or part.
    /// Created by calling <see cref="Vessel.Resources"/>,
    /// <see cref="Vessel.ResourcesInDecoupleStage"/> or
    /// <see cref="Parts.Part.Resources"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class Resources : Equatable<Resources>
    {
        readonly Guid vesselId;
        readonly int stage;
        readonly bool cumulative;
        readonly bool decoupleStage;
        readonly uint partId;
        // Note: 0 indicates no part

        internal Resources (global::Vessel vessel, int stage = -1, bool cumulative = true, bool decoupleStage = true)
        {
            vesselId = vessel.id;
            this.stage = stage;
            this.cumulative = cumulative;
            this.decoupleStage = decoupleStage;
        }

        internal Resources (Part part)
        {
            vesselId = Guid.Empty;
            stage = -1;
            cumulative = true;
            decoupleStage = true;
            partId = part.flightID;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Resources other)
        {
            return
            !ReferenceEquals (other, null) &&
            vesselId == other.vesselId &&
            stage == other.stage &&
            cumulative == other.cumulative &&
            decoupleStage == other.decoupleStage &&
            partId == other.partId;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode () ^ stage.GetHashCode () ^ cumulative.GetHashCode () ^ decoupleStage.GetHashCode () ^ partId.GetHashCode ();
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

        IList<int> ActivationStageNumbers ()
        {
            return InternalVessel.Parts
                .Where (part => part.hasStagingIcon)
                .Select (part => part.inverseStage)
                .Distinct ()
                .OrderBy (number => number)
                .ToList ();
        }

        int ActivationStageForPart (global::Part vesselPart, IList<int> activationStages)
        {
            if (vesselPart.hasStagingIcon)
                return vesselPart.inverseStage;

            var decoupleStage = vesselPart.DecoupledAt ();
            return activationStages
                .Where (stageNumber => stageNumber > decoupleStage)
                .DefaultIfEmpty (-1)
                .First ();
        }

        List<PartResource> PartResources {
            get {
                var resources = new List<PartResource> ();
                if (vesselId != Guid.Empty) {
                    var activationStages = decoupleStage ? null : ActivationStageNumbers ();
                    foreach (var vesselPart in InternalVessel.Parts) {
                        bool include;
                        if (decoupleStage) {
                            int d = vesselPart.DecoupledAt ();
                            include = (d == stage || (cumulative && d >= stage));
                        } else {
                            int s = ActivationStageForPart (vesselPart, activationStages);
                            include = (s == stage || (cumulative && s >= stage));
                        }
                        if (include) {
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
        }

        /// <summary>
        /// All the individual resources that can be stored.
        /// </summary>
        [KRPCProperty]
        public IList<Resource> All {
            get { return PartResources.Select (r => new Resource (r)).ToList (); }
        }

        /// <summary>
        /// All the individual resources with the given name that can be stored.
        /// </summary>
        [KRPCMethod]
        public IList<Resource> WithResource (string name)
        {
            return PartResources.Where (r => r.resourceName == name).Select (r => new Resource (r)).ToList ();
        }

        /// <summary>
        /// A list of resource names that can be stored.
        /// </summary>
        [KRPCProperty]
        public IList<string> Names {
            get {
                return PartResources.Select (r => r.resourceName).Distinct ().ToList ();
            }
        }

        /// <summary>
        /// Check whether the named resource can be stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public bool HasResource (string name)
        {
            return PartResources.Any (r => r.resourceName == name);
        }

        /// <summary>
        /// Returns the amount of a resource that can be stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public float Max (string name)
        {
            return PartResources.Where (r => r.resourceName == name).Sum (r => (float)r.maxAmount);
        }

        /// <summary>
        /// Returns the amount of a resource that is currently stored.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public float Amount (string name)
        {
            return PartResources.Where (r => r.resourceName == name).Sum (r => (float)r.amount);
        }

        static PartResourceDefinition GetResource (string name)
        {
            var resource = PartResourceLibrary.Instance.GetDefinition (name);
            if (resource == null)
                throw new ArgumentException ("Resource not found");
            return resource;
        }

        /// <summary>
        /// Returns the density of a resource, in <math>kg/l</math>.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public static float Density (string name)
        {
            return GetResource (name).density * 1000f;
        }

        /// <summary>
        /// Returns the flow mode of a resource.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        [KRPCMethod]
        public static ResourceFlowMode FlowMode (string name)
        {
            return GetResource (name).resourceFlowMode.ToResourceFlowMode ();
        }

        /// <summary>
        /// Whether use of all the resources are enabled.
        /// </summary>
        /// <remarks>
        /// This is <c>true</c> if all of the resources are enabled.
        /// If any of the resources are not enabled, this is <c>false</c>.
        /// </remarks>
        [KRPCProperty]
        public bool Enabled {
            get { return PartResources.All (resource => resource.flowState); }
            set {
                foreach (var resource in PartResources)
                    resource.flowState = value;
            }
        }
    }
}
