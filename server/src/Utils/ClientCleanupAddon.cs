using System.Collections.Generic;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Base class for addons that hold in-game state on behalf of RPC clients. Clears
    /// all of the addon's client-owned collections when the addon is created and
    /// destroyed (i.e. on scene changes), and provides Sweep to release state owned by
    /// disconnected clients, so that a disconnecting client cannot leave the game in a
    /// stuck state.
    /// </summary>
    /// <remarks>
    /// Subclasses call Sweep from their own Update or FixedUpdate, before acting on
    /// any client-owned state: addons that affect physics sweep at the top of
    /// FixedUpdate, so a disconnected client's state is never applied in the physics
    /// step in which the disconnect is detected; purely visual addons sweep in Update,
    /// which, unlike FixedUpdate, still runs while the game is paused. Subclasses that
    /// need their own Awake or OnDestroy must override them and call the base
    /// implementation (a same-name non-override method would silently shadow it).
    /// </remarks>
    public abstract class ClientCleanupAddon : MonoBehaviour
    {
        /// <summary>
        /// The client-owned collections managed by this addon.
        /// </summary>
        protected abstract IEnumerable<IClientOwnedCollection> Collections { get; }

        /// <summary>
        /// Release state owned by disconnected clients.
        /// </summary>
        protected void Sweep ()
        {
            foreach (var collection in Collections)
                collection.Sweep ();
        }

        /// <summary>
        /// Release all client-owned state.
        /// </summary>
        protected void Clear ()
        {
            foreach (var collection in Collections)
                collection.Clear ();
        }

        /// <summary>
        /// Wake the addon, releasing any state left over from a previous scene.
        /// </summary>
        protected virtual void Awake ()
        {
            Clear ();
        }

        /// <summary>
        /// Destroy the addon, releasing all client-owned state.
        /// </summary>
        protected virtual void OnDestroy ()
        {
            Clear ();
        }
    }
}
