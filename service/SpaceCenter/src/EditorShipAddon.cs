using UnityEngine;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Addon that hears the editor restoring a vessel from its own backup, which is what
    /// an undo or a redo is, so that the vessel it restores is recognized as the one the
    /// editor already had rather than as a new one.
    /// See <see cref="EditorExtensions.ShipGeneration" />.
    /// </summary>
    /// <remarks>
    /// The handlers are instance methods because the game cannot take a static one: the
    /// delegate is wrapped in an <c>EventData.EvtDelegate</c>, whose constructor reads the
    /// target the delegate was made over and raises a null reference for a delegate that
    /// has none. That throws out of whatever registered it, so a handler that never runs
    /// is what a static one looks like from the outside.
    /// </remarks>
    [KSPAddon (KSPAddon.Startup.AllGameScenes, false)]
    public sealed class EditorShipAddon : MonoBehaviour
    {
        /// <summary>
        /// Start listening for the editor restoring a vessel.
        /// </summary>
        public void Awake ()
        {
            GameEvents.onEditorUndo.Add (OnRestored);
            GameEvents.onEditorRedo.Add (OnRestored);
            GameEvents.onEditorRestoreState.Add (OnRestoredState);
        }

        /// <summary>
        /// Stop listening for the editor restoring a vessel.
        /// </summary>
        public void OnDestroy ()
        {
            GameEvents.onEditorUndo.Remove (OnRestored);
            GameEvents.onEditorRedo.Remove (OnRestored);
            GameEvents.onEditorRestoreState.Remove (OnRestoredState);
        }

        void OnRestored (ShipConstruct ship)
        {
            EditorExtensions.ShipRestored (ship);
        }

        void OnRestoredState ()
        {
            // The game raises this from the same call as the two above, once it has built
            // the vessel it restored, so the vessel the editor has now is that vessel.
            EditorExtensions.ShipRestored (EditorExtensions.Ship);
        }
    }
}
