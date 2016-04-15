using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An resource within an individual part.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Resource : Equatable<Resource>
    {
        readonly uint partId;
        readonly int resourceId;

        internal Resource (PartResource resource)
        {
            this.partId = resource.part.flightID;
            this.resourceId = resource.info.id;
        }

        /// <summary>
        /// Check if resource objects are equal.
        /// </summary>
        public override bool Equals (Resource obj)
        {
            return partId == obj.partId && resourceId == obj.resourceId;
        }

        /// <summary>
        /// Hash the resource object.
        /// </summary>
        public override int GetHashCode ()
        {
            return partId.GetHashCode () ^ resourceId.GetHashCode ();
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public global::Part InternalPart {
            get { return FlightGlobals.FindPartByID (partId); }
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public PartResource InternalResource {
            get { return InternalPart.Resources.Get (resourceId); }
        }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalResource.resourceName; }
        }

        /// <summary>
        /// The total amount of the resource that can be stored in the part.
        /// </summary>
        [KRPCProperty]
        public float Max {
            get { return (float)InternalResource.maxAmount; }
        }

        /// <summary>
        /// The amount of the resource that is currently stored in the part.
        /// </summary>
        [KRPCProperty]
        public float Amount {
            get { return (float)InternalResource.amount; }
        }

        /// <summary>
        /// The density of the resource, in <math>kg/l</math>.
        /// </summary>
        [KRPCProperty]
        public float Density {
            get { return Resources.Density (InternalResource.resourceName); }
        }

        /// <summary>
        /// The flow mode of the resource.
        /// </summary>
        [KRPCProperty]
        public ResourceFlowMode FlowMode {
            get { return Resources.FlowMode (InternalResource.resourceName); }
        }

        /// <summary>
        /// Whether use of this resource is enabled.
        /// </summary>
        [KRPCProperty]
        public bool Enabled {
            get { return InternalResource.flowMode != PartResource.FlowMode.None; }
            set { InternalResource.flowMode = (value ? PartResource.FlowMode.Both : PartResource.FlowMode.None); }
        }
    }
}
