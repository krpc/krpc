using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KRPC.Continuations;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Unity;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource drain.  Obtained by calling <see cref="Part.ResourceDrain"/>
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
            Part = part;
            var internalPart = part.InternalPart;
            drain = internalPart.Module<ModuleResourceDrain>();
            if (drain == null)
                throw new ArgumentException("Part is not a resource drain");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(ResourceDrain other)
        {
            return
            !ReferenceEquals(other, null) &&
            Part == other.Part &&
            drain.Equals(other.drain);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ drain.GetHashCode();

        }


        /// <summary>
        /// The KSP resource drain object.
        /// </summary>
        public ModuleResourceDrain InternalDrain
        {
            get { return drain; }
        }

        /// <summary>
        /// The part object for this resource drain
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Returns list of available resources
        /// </summary>
        [KRPCProperty]
        public List<Resource> AvailableResources
        {
            get
            {
                List<Resource> output = new List<Resource>();
                List<PartResource> PRs = drain.resourcesAvailable;
                foreach (PartResource PR in PRs)
                {
                    output.Add(new Resource(PR));
                }
                return output;
            }
        }

        /// <summary>
        /// Enable or Disable draining for the provided resource
        /// </summary>
        [KRPCMethod]
        public void SetResourceDrain(Resource R, bool b)
        {
            drain.TogglePartResource(R.InternalResource, b);
        }

        /// <summary>
        /// Checks whether the provided resource is selected for draining
        /// </summary>
        [KRPCMethod]
        public bool CheckResourceDrain(Resource R)
        {
            return drain.IsResourceDraining(R.InternalResource);
        }

        /// <summary>
        /// Possible modes for resource draining.
        /// 
        /// part mode drains only from the parent part.
        /// vessel mode drains from all available tanks.
        /// </summary>
        [KRPCEnum(Service = "SpaceCenter")]
        public enum DrainModes {
            /// <summary>
            /// Part
            /// </summary>
            part,
            /// <summary>
            /// Vessel
            /// </summary>
            vessel
        }

        /// <summary>
        /// Sets drain mode to part or vessel-wide
        /// </summary>
        [KRPCProperty]
        public DrainModes DrainMode
        {
            get
            {
                if (drain.flowMode) return DrainModes.vessel;
                else return DrainModes.part;
            }
            set
            {
                if (value == DrainModes.vessel) drain.flowMode = true;
                else drain.flowMode = false;
            }
        }

        /// <summary>
        /// Maximum possible rate of draining.
        /// </summary>
        [KRPCProperty]
        public float MaxDrainRate { get { return drain.maxDrainRate; } }

        /// <summary>
        /// Minimum possible rate of draining
        /// </summary>
        [KRPCProperty]
        public float MinDrainRate { get { return drain.minDrainRate; } }

        /// <summary>
        ///  Current rate of draining
        /// </summary>
        [KRPCProperty]
        public float DrainRate { get { return drain.drainRate; } set { drain.drainRate = value; } }

        /// <summary>
        /// Activates resource drain for all enabled parts
        /// </summary>
        [KRPCMethod]
        public void Start()
        {
            drain.TurnOnDrain();
        }

        /// <summary>
        /// Turns off resource drain
        /// </summary>
        [KRPCMethod]
        public void Stop()
        {
            drain.TurnOffDrain();
        }


    }
}