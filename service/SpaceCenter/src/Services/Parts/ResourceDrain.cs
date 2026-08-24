using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Unity;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource drain. Obtained by calling <see cref="Part.ResourceDrain"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ResourceDrain : Equatable<ResourceDrain>, IGameObjectState
    {
        ModuleRef drainRef;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<ModuleResourceDrain>();
        }

        internal ResourceDrain(Part part)
        {
            if (!Is (part))
                throw new ArgumentException ("Part is not a resource drain");
            Part = part;
            var internalPart = part.InternalPart;
            drainRef = ModuleRef.ForType<ModuleResourceDrain> (internalPart);
        }

        ModuleResourceDrain InternalDrain {
            get { return (ModuleResourceDrain)drainRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the resource drain: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return drainRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(ResourceDrain other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && drainRef == other.drainRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Of (Part).And (drainRef);
        }

        /// <summary>
        /// The part object for this resource drain.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// List of available resources.
        /// </summary>
        [KRPCProperty]
        public List<Resource> AvailableResources
        {
            get { return InternalDrain.resourcesAvailable.Select(x => new Resource(x)).ToList(); }
        }

        /// <summary>
        /// Whether the given resource should be drained.
        /// </summary>
        [KRPCMethod]
        public void SetResource(Resource resource, bool enabled)
        {
            InternalDrain.TogglePartResource(resource.InternalResource, enabled);
        }

        /// <summary>
        /// Whether the provided resource is enabled for draining.
        /// </summary>
        [KRPCMethod]
        public bool CheckResource(Resource resource)
        {
            return InternalDrain.IsResourceDraining(resource.InternalResource);
        }

        /// <summary>
        /// The drain mode.
        /// </summary>
        [KRPCProperty]
        public DrainMode DrainMode
        {
            get { return InternalDrain.flowMode ? DrainMode.Vessel : DrainMode.Part; }
            set { InternalDrain.flowMode = (value == DrainMode.Vessel); }
        }

        /// <summary>
        /// Maximum possible drain rate.
        /// </summary>
        [KRPCProperty]
        public float MaxRate { get { return InternalDrain.maxDrainRate; } }

        /// <summary>
        /// Minimum possible drain rate
        /// </summary>
        [KRPCProperty]
        public float MinRate { get { return InternalDrain.minDrainRate; } }

        /// <summary>
        /// Current drain rate.
        /// </summary>
        [KRPCProperty]
        public float Rate {
            get { return InternalDrain.drainRate; }
            set { InternalDrain.drainRate = value; }
        }

        /// <summary>
        /// Activates resource draining for all enabled parts.
        /// </summary>
        [KRPCMethod]
        public void Start()
        {
            InternalDrain.TurnOnDrain();
        }

        /// <summary>
        /// Turns off resource draining.
        /// </summary>
        [KRPCMethod]
        public void Stop()
        {
            InternalDrain.TurnOffDrain();
        }
    }
}
