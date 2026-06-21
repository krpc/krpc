using System;
using System.Linq;
using KRPC.SpaceCenter.ExtensionMethods;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Abstraction over the various ways a KSP part can implement an engine thrust
    /// reverser. KSP does not expose a standard reverser capability, so each supported
    /// mechanism (stock and recognized mods) is wrapped by its own adapter. Engines
    /// whose reverser is not recognized have no adapter, and so report
    /// <see cref="Engine.CanReverseThrust"/> as <c>false</c> rather than guessing.
    /// </summary>
    interface IThrustReverser
    {
        /// <summary>
        /// Whether thrust is currently reversed.
        /// </summary>
        bool Reversed { get; set; }

        /// <summary>
        /// Toggle the thrust reverser between forward and reversed.
        /// </summary>
        void Toggle ();
    }

    /// <summary>
    /// Recognizes the thrust reverser, if any, on an engine part and wraps it in an
    /// <see cref="IThrustReverser"/> adapter.
    /// </summary>
    static class ThrustReverser
    {
        /// <summary>
        /// Returns an adapter for the part's thrust reverser, or <c>null</c> if the part
        /// has no recognized reverser.
        /// </summary>
        internal static IThrustReverser Create (global::Part part)
        {
            // Stock and recognized-mod reversers implemented as a ModuleAnimateGeneric
            // whose animation rotates or repositions the engine's thrust transform.
            foreach (var animation in part.Modules.OfType<ModuleAnimateGeneric> ()) {
                bool reversedWhenDeployed;
                if (IsAnimationReverser (animation, part, out reversedWhenDeployed))
                    return new AnimationThrustReverser (animation, reversedWhenDeployed);
            }
            // Recognized mod modules that reverse thrust by switching the active thrust
            // transform, driven through the stock part module API by field/event/action
            // name so kRPC does not need a compile-time reference to the mod.
            var firespitter = part.Module ("FSswitchEngineThrustTransform");
            if (firespitter != null && FirespitterCanReverse (firespitter))
                return new FirespitterThrustReverser (firespitter);
            var propSpinner = part.Module ("WBIPropSpinner");
            if (propSpinner != null && PropSpinnerCanReverse (propSpinner))
                return new PropSpinnerThrustReverser (propSpinner);
            return null;
        }

        /// <summary>
        /// Whether the given animation module is a recognized thrust reverser. The
        /// animation name is not localized, but is part-specific, so several signatures
        /// are matched. <paramref name="reversedWhenDeployed"/> records which end of the
        /// animation is the reversed state; this cannot be inferred from the name.
        /// </summary>
        static bool IsAnimationReverser (ModuleAnimateGeneric animation, global::Part part, out bool reversedWhenDeployed)
        {
            // The deployed end of the animation is the reversed state for every part
            // recognized below.
            reversedWhenDeployed = true;
            // A semantic module identifier, used by e.g. WaterDrinker, is the most
            // reliable signal as it is independent of the model's animation name.
            if (animation.moduleID == "Reverser")
                return true;
            // Stock turbofans ("TF1ThrustReverser", "TF2ThrustReverser"), Aircraft
            // Carrier Accessories ("ThrustReverser"), Mk3 Expansion ("ThrustReverse",
            // "XLTFan_ReverseThrust"), Neist Airliner ("...ReverseThrust"), etc.
            var name = animation.animationName ?? string.Empty;
            if (name.IndexOf ("reverse", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            // Orbital Tug, whose reverser animation name does not mention "reverse";
            // matched explicitly by part to avoid claiming unrelated animations.
            if (name.Equals ("rotateThrust", StringComparison.OrdinalIgnoreCase) &&
                (part.partInfo.name == "engineOnArm" || part.partInfo.name == "engineOnArmLFO"))
                return true;
            return false;
        }

        /// <summary>
        /// Whether a Firespitter FSswitchEngineThrustTransform module exposes everything
        /// the adapter drives: the state field, and the actions used to set and toggle it.
        /// </summary>
        static bool FirespitterCanReverse (global::PartModule module)
        {
            return module.Fields ["isReversed"] != null &&
                module.Actions ["reverseTTAction"] != null &&
                module.Actions ["normalTTAction"] != null &&
                module.Actions ["switchTTAction"] != null;
        }

        /// <summary>
        /// Whether a WBIPropSpinner module can reverse thrust and exposes everything the
        /// adapter drives. The module is present on propellers that cannot necessarily
        /// reverse, so its capability flag is checked along with the state field and the
        /// event used to toggle it.
        /// </summary>
        static bool PropSpinnerCanReverse (global::PartModule module)
        {
            var canReverseThrust = module.Fields ["canReverseThrust"];
            return module.Fields ["reverseThrust"] != null &&
                canReverseThrust != null && (bool)canReverseThrust.GetValue (module) &&
                module.Events ["ToggleThrustTransform"] != null;
        }
    }

    /// <summary>
    /// Adapter for a thrust reverser implemented as a stock ModuleAnimateGeneric.
    /// </summary>
    sealed class AnimationThrustReverser : IThrustReverser
    {
        readonly ModuleAnimateGeneric animation;
        readonly bool reversedWhenDeployed;

        internal AnimationThrustReverser (ModuleAnimateGeneric animation, bool reversedWhenDeployed)
        {
            this.animation = animation;
            this.reversedWhenDeployed = reversedWhenDeployed;
        }

        public bool Reversed {
            get { return (animation.animTime > 0.5f) == reversedWhenDeployed; }
            set { if (value != Reversed) animation.Toggle (); }
        }

        public void Toggle ()
        {
            animation.Toggle ();
        }
    }

    /// <summary>
    /// Adapter for the Firespitter FSswitchEngineThrustTransform module, which reverses
    /// thrust by switching the engine's active thrust transform.
    /// </summary>
    sealed class FirespitterThrustReverser : IThrustReverser
    {
        readonly global::PartModule module;

        internal FirespitterThrustReverser (global::PartModule module)
        {
            this.module = module;
        }

        public bool Reversed {
            get { return (bool)module.Fields ["isReversed"].GetValue (module); }
            set {
                module.Actions [value ? "reverseTTAction" : "normalTTAction"].Invoke (
                    new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
            }
        }

        public void Toggle ()
        {
            module.Actions ["switchTTAction"].Invoke (
                new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate));
        }
    }

    /// <summary>
    /// Adapter for the KerbalActuators WBIPropSpinner module, which reverses thrust by
    /// switching between a forward and a reverse thrust transform.
    /// </summary>
    sealed class PropSpinnerThrustReverser : IThrustReverser
    {
        readonly global::PartModule module;

        internal PropSpinnerThrustReverser (global::PartModule module)
        {
            this.module = module;
        }

        public bool Reversed {
            get { return (bool)module.Fields ["reverseThrust"].GetValue (module); }
            // The module only exposes a toggle, so set the desired state by toggling
            // when it differs from the current one.
            set { if (value != Reversed) Toggle (); }
        }

        public void Toggle ()
        {
            module.Events ["ToggleThrustTransform"].Invoke ();
        }
    }
}
