using System;
using System.Text.RegularExpressions;
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
        readonly Regex numberRegex = new Regex (@"(\d+(\.\d+)?)");

        internal static bool Is (Part part)
        {
            return
            part.InternalPart.HasModule<ModuleResourceHarvester> () &&
            part.InternalPart.HasModule<ModuleAnimationGroup> ();
        }

        internal ResourceHarvester (Part part)
        {
            this.part = part;
            harvester = part.InternalPart.Module<ModuleResourceHarvester> ();
            animator = part.InternalPart.Module<ModuleAnimationGroup> ();
            if (harvester == null || animator == null)
                throw new ArgumentException ("Part is not a resource harvester");
        }

        /// <summary>
        /// Check if resource harvesters are equal.
        /// </summary>
        public override bool Equals (ResourceHarvester obj)
        {
            return part == obj.part && harvester == obj.harvester && animator == obj.animator;
        }

        /// <summary>
        /// Hash the resource harvester.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ harvester.GetHashCode () ^ animator.GetHashCode ();
        }

        /// <summary>
        /// The part object for this harvester.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The state of the harvester.
        /// </summary>
        [KRPCProperty]
        public ResourceHarvesterState State {
            get {
                if (harvester.IsActivated)
                    return ResourceHarvesterState.Active;
                else if (animator.ActiveAnimation.isPlaying)
                    return animator.isDeployed ? ResourceHarvesterState.Deploying : ResourceHarvesterState.Retracting;
                else if (animator.isDeployed)
                    return ResourceHarvesterState.Deployed;
                else
                    return ResourceHarvesterState.Retracted;
            }
        }

        /// <summary>
        /// Whether the harvester is deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return State == ResourceHarvesterState.Deployed || State == ResourceHarvesterState.Active; }
            set {
                if (value && !animator.isDeployed)
                    animator.DeployModule ();
                if (!value && animator.isDeployed)
                    animator.RetractModule ();
            }
        }

        /// <summary>
        /// Whether the harvester is actively drilling.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return State == ResourceHarvesterState.Active; }
            set {
                if (!Deployed)
                    return;
                if (value && !harvester.IsActivated)
                    harvester.StartResourceConverter ();
                if (!value && harvester.IsActivated)
                    harvester.StopResourceConverter ();
            }
        }

        /// <summary>
        /// The rate at which the drill is extracting ore, in units per second.
        /// </summary>
        [KRPCProperty]
        public float ExtractionRate {
            get {
                if (!Deployed || !Active)
                    return 0;
                var status = harvester.ResourceStatus;
                Match match = numberRegex.Match (status);
                if (!match.Success)
                    return 0;
                return Single.Parse (match.Groups [1].Value);
            }
        }

        /// <summary>
        /// The thermal efficiency of the drill, as a percentage of its maximum.
        /// </summary>
        [KRPCProperty]
        public float ThermalEfficiency {
            get {
                if (!Deployed || !Active)
                    return 0;
                var status = harvester.status;
                if (!status.Contains ("load"))
                    return 0;
                Match match = numberRegex.Match (status);
                if (!match.Success)
                    return 0;
                return Single.Parse (match.Groups [1].Value);
            }
        }

        /// <summary>
        /// The core temperature of the drill, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float CoreTemperature {
            get { return (float)harvester.GetCoreTemperature (); }
        }

        /// <summary>
        /// The core temperature at which the drill will operate with peak efficiency, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float OptimumCoreTemperature {
            get { return (float)harvester.GetGoalTemperature (); }
        }
    }
}
