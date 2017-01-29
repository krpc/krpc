using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource harvester (drill). Obtained by calling <see cref="Part.ResourceHarvester"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class ResourceHarvester : Equatable<ResourceHarvester>
    {
        readonly ModuleResourceHarvester harvester;
        readonly ModuleAnimationGroup animator;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return
            internalPart.HasModule<ModuleResourceHarvester> () &&
            internalPart.HasModule<ModuleAnimationGroup> ();
        }

        internal ResourceHarvester (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            harvester = internalPart.Module<ModuleResourceHarvester> ();
            animator = internalPart.Module<ModuleAnimationGroup> ();
            if (harvester == null || animator == null)
                throw new ArgumentException ("Part is not a resource harvester");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ResourceHarvester other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            (harvester == other.harvester || harvester.Equals (other.harvester));
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ harvester.GetHashCode ();
        }

        /// <summary>
        /// The part object for this harvester.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

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
            get {
                var state = State;
                return state == ResourceHarvesterState.Deployed || state == ResourceHarvesterState.Active;
            }
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
                if (!Active)
                    return 0;
                return GetFloatValue (harvester.ResourceStatus);
            }
        }

        /// <summary>
        /// The thermal efficiency of the drill, as a percentage of its maximum.
        /// </summary>
        [KRPCProperty]
        public float ThermalEfficiency {
            get {
                if (!Active)
                    return 0;
                var status = harvester.status;
                if (!status.Contains ("load"))
                    return 0;
                return GetFloatValue (status);
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

        static readonly Regex numberRegex = new Regex (@"(\d+(\.\d+)?)");

        static float GetFloatValue (string value)
        {
            Match match = numberRegex.Match (value);
            if (!match.Success)
                return 0;
            float result;
            if (!float.TryParse (match.Groups [1].Value, out result))
                return 0;
            return result;
        }
    }
}
