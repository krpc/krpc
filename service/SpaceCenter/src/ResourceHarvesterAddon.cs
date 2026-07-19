using System.Collections.Generic;
using System.Linq;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that applies a harvester active-state requested while the harvester
    /// was still deploying.
    /// </summary>
    /// <remarks>
    /// A drill's resource converter can only be started once its deploy animation
    /// has finished, which takes several seconds. A write to
    /// <see cref="Services.Parts.ResourceHarvester.Active"/> issued in that window
    /// (e.g. setting <c>Deployed</c> then <c>Active</c> back to back) cannot be
    /// applied immediately, so the requested state is recorded here and applied
    /// when the deploy completes. A request is discarded if the deploy is canceled
    /// (the drill starts retracting) or the part is destroyed. There is one
    /// requested state per drill, held in a static table (last writer wins).
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public sealed class ResourceHarvesterAddon : UnityEngine.MonoBehaviour
    {
        sealed class PendingActivation
        {
            public ModuleAnimationGroup Animator;
            public bool Active;
        }

        static readonly Dictionary<ModuleResourceHarvester, PendingActivation> pending =
            new Dictionary<ModuleResourceHarvester, PendingActivation> ();

        /// <summary>
        /// Record the active-state to apply to a harvester once its deploy completes.
        /// A superseding request for the same harvester overwrites the entry.
        /// </summary>
        internal static void Request (ModuleResourceHarvester harvester, ModuleAnimationGroup animator, bool active)
        {
            pending [harvester] = new PendingActivation {
                Animator = animator,
                Active = active
            };
        }

        /// <summary>
        /// Discard any pending request for a harvester. Called when a request is
        /// applied directly, so an older deferred write cannot override it later.
        /// </summary>
        internal static void Cancel (ModuleResourceHarvester harvester)
        {
            pending.Remove (harvester);
        }

        /// <summary>
        /// Wake the addon.
        /// </summary>
        public void Awake ()
        {
            pending.Clear ();
        }

        /// <summary>
        /// Destroy the addon.
        /// </summary>
        public void OnDestroy ()
        {
            pending.Clear ();
        }

        /// <summary>
        /// Apply pending activation requests for harvesters whose deploy has completed.
        /// </summary>
        public void FixedUpdate ()
        {
            if (pending.Count == 0)
                return;
            foreach (var harvester in pending.Keys.ToList ()) {
                var entry = pending [harvester];
                if (harvester == null || entry.Animator == null || !entry.Animator.isDeployed) {
                    // The part was destroyed, or the deploy was canceled by a retract.
                    pending.Remove (harvester);
                    continue;
                }
                var animation = entry.Animator.ActiveAnimation;
                if (animation != null && animation.isPlaying)
                    // Still deploying.
                    continue;
                if (entry.Active && !harvester.IsActivated)
                    harvester.StartResourceConverter ();
                else if (!entry.Active && harvester.IsActivated)
                    harvester.StopResourceConverter ();
                pending.Remove (harvester);
            }
        }
    }
}
