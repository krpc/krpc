using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    [KRPCClass(Service = "SpaceCenter")]
    public class ResourceDrain : Equatable<ResourceDrain>
    {
        readonly ModuleResourceDrain drain;

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
            drain = internalPart.Module<ModuleResourceDrain>();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(ResourceDrain other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && drain == other.drain;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ drain.GetHashCode();
        }

        /// <summary>
        /// The part object for this resource drain.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// List of available resources.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeGenericListsRule")]
        public List<Resource> AvailableResources
        {
            get { return drain.resourcesAvailable.Select(x => new Resource(x)).ToList(); }
        }

        /// <summary>
        /// Whether the given resource should be drained.
        /// </summary>
        [KRPCMethod]
        public void SetResource(Resource resource, bool enabled)
        {
            if (ReferenceEquals (resource, null))
                throw new ArgumentNullException (nameof (resource));
            drain.TogglePartResource(resource.InternalResource, enabled);
        }

        /// <summary>
        /// Whether the provided resource is enabled for draining.
        /// </summary>
        [KRPCMethod]
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public bool CheckResource(Resource resource)
        {
            if (ReferenceEquals (resource, null))
                throw new ArgumentNullException (nameof (resource));
            return drain.IsResourceDraining(resource.InternalResource);
        }

        /// <summary>
        /// The drain mode.
        /// </summary>
        [KRPCProperty]
        public DrainMode DrainMode
        {
            get { return drain.flowMode ? DrainMode.Vessel : DrainMode.Part; }
            set { drain.flowMode = (value == DrainMode.Vessel); }
        }

        /// <summary>
        /// Maximum possible drain rate.
        /// </summary>
        [KRPCProperty]
        public float MaxRate { get { return drain.maxDrainRate; } }

        /// <summary>
        /// Minimum possible drain rate
        /// </summary>
        [KRPCProperty]
        public float MinRate { get { return drain.minDrainRate; } }

        /// <summary>
        /// Current drain rate.
        /// </summary>
        [KRPCProperty]
        public float Rate {
            get { return drain.drainRate; }
            set { drain.drainRate = value; }
        }

        /// <summary>
        /// Activates resource draining for all enabled parts.
        /// </summary>
        [KRPCMethod]
        public void Start()
        {
            drain.TurnOnDrain();
        }

        /// <summary>
        /// Turns off resource draining.
        /// </summary>
        [KRPCMethod]
        public void Stop()
        {
            drain.TurnOffDrain();
        }
    }
}
