using System;
using System.Reflection;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource harvester (drill). Obtained by calling <see cref="Part.ResourceHarvester"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ResourceHarvester : Equatable<ResourceHarvester>, IGameObjectState
    {
        ModuleRef harvesterRef;
        ModuleRef animatorRef;

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
            harvesterRef = ModuleRef.ForType<ModuleResourceHarvester> (internalPart);
            animatorRef = ModuleRef.ForType<ModuleAnimationGroup> (internalPart);
            if (harvesterRef.Find (internalPart) == null || animatorRef.Find (internalPart) == null)
                throw new ArgumentException ("Part is not a resource harvester");
        }

        ModuleResourceHarvester InternalHarvester {
            get { return (ModuleResourceHarvester)harvesterRef.Get (Part.InternalPart); }
        }

        ModuleAnimationGroup InternalAnimator {
            get { return (ModuleAnimationGroup)animatorRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the part carrying the resource harvester, or destroyed once that
        /// part loses the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return harvesterRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ResourceHarvester other)
        {
            return
            !ReferenceEquals (other, null) &&
            Part == other.Part &&
            harvesterRef == other.harvesterRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (harvesterRef);
        }

        /// <summary>
        /// The part object for this harvester.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// The deployment state of the harvester. Whether it is drilling is
        /// reported separately by <see cref="Active" />.
        /// </summary>
        /// <remarks>
        /// A harvester is never <see cref="DeployableState.Broken" />, as the game
        /// does not track damage for them.
        /// </remarks>
        [KRPCProperty]
        public DeployableState State {
            get {
                // An activated harvester is necessarily fully deployed. This has to be
                // checked first, as the drill keeps animating while it is running and
                // so the animation alone cannot tell operation from deployment.
                if (InternalHarvester.IsActivated)
                    return DeployableState.Deployed;
                // ActiveAnimation can be null/destroyed (e.g. while the vessel is
                // packed); guard it so the state can still be reported.
                var animation = InternalAnimator.ActiveAnimation;
                if (animation != null && animation.isPlaying)
                    return InternalAnimator.isDeployed ? DeployableState.Deploying : DeployableState.Retracting;
                return InternalAnimator.isDeployed ? DeployableState.Deployed : DeployableState.Retracted;
            }
        }

        /// <summary>
        /// Whether the harvester is deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return State == DeployableState.Deployed; }
            set {
                if (value && !InternalAnimator.isDeployed)
                    InternalAnimator.DeployModule ();
                if (!value && InternalAnimator.isDeployed)
                    InternalAnimator.RetractModule ();
            }
        }

        /// <summary>
        /// Whether the harvester is actively drilling.
        /// </summary>
        /// <remarks>
        /// A value set while the harvester is deploying is applied when the
        /// deploy completes, so it can be set immediately after setting
        /// <see cref="Deployed"/> without waiting for the deploy animation.
        /// Setting it has no effect while the harvester is retracted or
        /// retracting.
        /// </remarks>
        [KRPCProperty]
        public bool Active {
            get { return InternalHarvester.IsActivated; }
            set {
                if (!Deployed) {
                    // The converter cannot start until the deploy animation has finished,
                    // so defer the requested state until then
                    if (State == DeployableState.Deploying)
                        ResourceHarvesterAddon.Request (InternalHarvester, InternalAnimator, value);
                    return;
                }
                ResourceHarvesterAddon.Cancel (InternalHarvester);
                if (value && !InternalHarvester.IsActivated)
                    InternalHarvester.StartResourceConverter ();
                if (!value && InternalHarvester.IsActivated)
                    InternalHarvester.StopResourceConverter ();
            }
        }

        static readonly FieldInfo resFlowField =
            typeof (ModuleResourceHarvester).GetField ("_resFlow", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// The rate at which the drill is extracting ore, in units per second.
        /// </summary>
        [KRPCProperty]
        public float ExtractionRate {
            get {
                if (!Active)
                    return 0;
                return Convert.ToSingle (resFlowField.GetValue (InternalHarvester));
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
                // Evaluate the thermal efficiency curve at the current core
                // temperature, matching ResourceConverter.ThermalEfficiency. The
                // harvester's "status" string is not used: a drill never reports a
                // "<x>% load" status, and its "Ore rate" readout is only populated
                // while the part's right-click window is open.
                var temp = Convert.ToSingle (InternalHarvester.GetCoreTemperature ());
                return InternalHarvester.ThermalEfficiency.Evaluate (temp);
            }
        }

        /// <summary>
        /// The core temperature of the drill, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float CoreTemperature {
            get { return (float)InternalHarvester.GetCoreTemperature (); }
        }

        /// <summary>
        /// The core temperature at which the drill will operate with peak efficiency, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float OptimumCoreTemperature {
            get { return (float)InternalHarvester.GetGoalTemperature (); }
        }
    }
}
