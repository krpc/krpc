using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ResourceHarvester"/>
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

        public override bool Equals (ResourceHarvester obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this Harvester
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// Returns true if the harvester is deployed and ready to drill, or currently drilling.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return animator.isDeployed; }
        }

        /// <summary>
        /// Returns true if the harvester is deployed and ready to drill, or currently drilling.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return harvester.IsActivated; }
        }

        /// <summary>
        /// Deploys the harvester. Has no effect if already deployed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            if (harvester != null && animator != null)
                animator.DeployModule ();
        }

        /// <summary>
        /// Retracts the harvester. Has no effect if already retracted.
        /// </summary>
        [KRPCMethod]
        public void Retract ()
        {
            if (harvester != null && animator != null)
                animator.RetractModule ();
        }

        /// <summary>
        /// Start the harvester.
        /// </summary>
        [KRPCMethod]
        public void Start ()
        {
            if (harvester != null)
                harvester.StartResourceConverter ();
        }

        /// <summary>
        /// Stop the harvester.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            if (harvester != null)
                harvester.StopResourceConverter ();
        }
    }
}
