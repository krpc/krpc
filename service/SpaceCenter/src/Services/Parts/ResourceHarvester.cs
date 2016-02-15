using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ResourceHarvester"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ResourceHarvester : Equatable<ResourceHarvester>
    {
        readonly Part part;
        readonly ModuleResourceHarvester harvester;
        readonly ModuleAnimationGroup animator;

        internal ResourceHarvester (Part part)
        {
            this.part = part;
            harvester = part.InternalPart.Module<ModuleResourceHarvester> ();
            animator = part.InternalPart.Module<ModuleAnimationGroup> ();
            if (harvester == null)
                throw new ArgumentException ("Part has no Resource Harvester Module");
        }

        /// <summary>
        /// Check if resource harvesters are equal.
        /// </summary>
        public override bool Equals (ResourceHarvester obj)
        {
            return part == obj.part;
        }

        /// <summary>
        /// Hash the resource harvester.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this harvester.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// True if the harvester is deployed and ready to drill, or currently drilling.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return animator.isDeployed; }
        }

        /// <summary>
        /// True if the harvester is active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return harvester.IsActivated; }
        }

        /// <summary>
        /// Deploy the harvester. Has no effect if the harvester is already deployed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            if (animator != null)
                animator.DeployModule ();
        }

        /// <summary>
        /// Retracts the harvester. Has no effect if the harvester iis already retracted.
        /// </summary>
        [KRPCMethod]
        public void Retract ()
        {
            if (animator != null)
                animator.RetractModule ();
        }

        /// <summary>
        /// Start the harvester.
        /// </summary>
        [KRPCMethod]
        public void Start ()
        {
            harvester.StartResourceConverter ();
        }

        /// <summary>
        /// Stop the harvester.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            harvester.StopResourceConverter ();
        }
    }
}
