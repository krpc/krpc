using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// An individual resource stored within a part.
    /// Created using methods in the <see cref="Resources"/> class.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Resource : Equatable<Resource>
    {
        readonly uint partId;
        readonly int resourceId;

        internal Resource (PartResource resource)
        {
            partId = resource.part.flightID;
            resourceId = resource.info.id;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Resource other)
        {
            return !ReferenceEquals (other, null) && partId == other.partId && resourceId == other.resourceId;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return partId.GetHashCode () ^ resourceId.GetHashCode ();
        }

        /// <summary>
        /// The KSP part.
        /// </summary>
        public Part InternalPart {
            get { return FlightGlobals.FindPartByID (partId); }
        }

        /// <summary>
        /// The KSP part resource.
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
        /// The part containing the resource.
        /// </summary>
        [KRPCProperty]
        public Parts.Part Part {
            get { return new Parts.Part (InternalPart); }
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
            get { return InternalResource.flowState; }
            set { InternalResource.flowState = value; }
        }
    }
}
