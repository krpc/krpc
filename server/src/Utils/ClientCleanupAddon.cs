using System.Collections.Generic;
using UnityEngine;

namespace KRPC.Utils
{
    /// <summary>
    /// Base class for addons that hold in-game state on behalf of RPC clients. Gives up
    /// all of the addon's client-owned collections when the addon is created, which is on
    /// entering a game scene, and provides Sweep to release state owned by disconnected
    /// clients, so that a disconnecting client cannot leave the game in a stuck state.
    /// </summary>
    /// <remarks>
    /// Subclasses call Sweep from their own Update or FixedUpdate, before acting on
    /// any client-owned state: addons that affect physics sweep at the top of
    /// FixedUpdate, so a disconnected client's state is never applied in the physics
    /// step in which the disconnect is detected; purely visual addons sweep in Update,
    /// which, unlike FixedUpdate, still runs while the game is paused. An addon has to
    /// sweep for the state of the previous scene to be released at all. Subclasses that
    /// need their own Awake must override it and call the base implementation (a
    /// same-name non-override method would silently shadow it).
    ///
    /// Nothing is released when a scene is left. An addon that runs in only some scenes,
    /// a flight-only one for instance, therefore holds the state of the scene it last ran
    /// in until it next runs and sweeps, which can be a long time later or never. That
    /// state stays in the collections and a client can still reach it in the meantime, so
    /// a release action has to tolerate its subject having been destroyed by the scene
    /// being unloaded. The game objects behind it are taken away by that unload whether or
    /// not the release has run, unless they are ones the game keeps across a scene change.
    /// </remarks>
    public abstract class ClientCleanupAddon : MonoBehaviour
    {
        /// <summary>
        /// The client-owned collections managed by this addon.
        /// </summary>
        protected abstract IEnumerable<IClientOwnedCollection> Collections { get; }

        /// <summary>
        /// Whether the state left over from the previous scene is still waiting to be
        /// released by the first sweep of this one.
        /// </summary>
        bool releasePending;

        /// <summary>
        /// Release state owned by disconnected clients, along with the state left over
        /// from the previous scene the first time this is called in the current one.
        /// </summary>
        protected void Sweep ()
        {
            if (releasePending) {
                releasePending = false;
                foreach (var collection in Collections)
                    collection.ReleaseDetached ();
            }
            foreach (var collection in Collections)
                collection.Sweep ();
        }

        /// <summary>
        /// Wake the addon, giving up the state left over from the previous scene.
        /// </summary>
        /// <remarks>
        /// The state is taken out of the collections here, so a client cannot reach it
        /// any more, but it is released on the first sweep of this scene rather than now.
        /// A game object the game keeps across a scene change, such as anything parented
        /// to the stock user interface canvas, is not taken away by a destroy asked for
        /// while a scene is loading: it lives on into the scene being entered and is only
        /// collected by the one after that. Asking once the scene is running takes effect
        /// on the next frame. Taking the state out now and releasing it later also means
        /// whatever a client adds in between is not released along with it.
        /// </remarks>
        protected virtual void Awake ()
        {
            foreach (var collection in Collections)
                collection.Detach ();
            releasePending = true;
        }
    }
}
