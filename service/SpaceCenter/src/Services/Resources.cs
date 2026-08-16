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
    /// <see cref="Vessel.ResourcesInDecoupleStage"/>, <see cref="EditorVessel.Resources"/>
    /// or <see cref="Parts.Part.Resources"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight | GameScene.Editor)]
    public class Resources : Equatable<Resources>, IGameObjectState
    {
        /// <summary>
        /// The id of the vessel the resources belong to, or <c>Guid.Empty</c> when they
        /// belong to the vessel in the editor or to a single part.
        /// </summary>
        readonly Guid vesselId;

        /// <summary>
        /// Whether the resources are those of the vessel under construction in the
        /// editor. The editor only ever holds one vessel, so it needs no identifier.
        /// </summary>
        readonly bool editorVessel;

        readonly int stage;
        readonly bool cumulative;
        readonly bool decoupleStage;

        /// <summary>
        /// The part the resources belong to, when they are a single part's.
        /// </summary>
        readonly PartId partId;
        readonly bool hasPart;

        internal Resources (global::Vessel vessel, int stage = -1, bool cumulative = true, bool decoupleStage = true)
        {
            vesselId = vessel.id;
            this.stage = stage;
            this.cumulative = cumulative;
            this.decoupleStage = decoupleStage;
        }

        internal Resources (int stage = -1, bool cumulative = true, bool decoupleStage = true)
        {
            editorVessel = true;
            this.stage = stage;
            this.cumulative = cumulative;
            this.decoupleStage = decoupleStage;
        }

        internal Resources (Part part)
        {
            stage = -1;
            cumulative = true;
            decoupleStage = true;
            partId = new PartId (part);
            hasPart = true;
        }

        /// <summary>
        /// What the game holds for the vessel or part these are the resources of.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                if (hasPart)
                    return partId.GameObjectState;
                return editorVessel
                    ? EditorExtensions.ShipState
                    : FlightGlobalsExtensions.VesselState (vesselId);
            }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Resources other)
        {
            return
            !ReferenceEquals (other, null) &&
            vesselId == other.vesselId &&
            editorVessel == other.editorVessel &&
            stage == other.stage &&
            cumulative == other.cumulative &&
            decoupleStage == other.decoupleStage &&
            hasPart == other.hasPart &&
            partId == other.partId;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vesselId.GetHashCode () ^ editorVessel.GetHashCode () ^ stage.GetHashCode () ^
            cumulative.GetHashCode () ^ decoupleStage.GetHashCode () ^ partId.GetHashCode ();
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
        /// The KSP vessel in flight, or ship construct in the editor, that the resources
        /// are drawn from, when they are not a single part's.
        /// </summary>
        IShipconstruct InternalShipConstruct {
            get {
                return editorVessel ? (IShipconstruct)EditorExtensions.GetShip () : InternalVessel;
            }
        }

        /// <summary>
        /// The stock delta-v simulation for the vessel, which the activation stage numbers
        /// are taken from. Null when there is none, in which case they fall back to the
        /// parts' staging icons.
        /// </summary>
        VesselDeltaV InternalDeltaV {
            get {
                return editorVessel
                    ? EditorExtensions.GetShip ().vesselDeltaV
                    : InternalVessel.VesselDeltaV;
            }
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public Part InternalPart {
            get {
                if (!hasPart)
                    throw new InvalidOperationException ("Resources object has no part");
                return partId.Resolve ();
            }
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
                if (!hasPart) {
                    var allParts = InternalShipConstruct.Parts;
                    var activationStages = decoupleStage
                        ? null
                        : StagingExtensions.ActivationStageNumbers (InternalDeltaV, allParts);
                    foreach (var vesselPart in allParts) {
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
                } else {
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
