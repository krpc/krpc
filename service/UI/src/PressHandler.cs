using UnityEngine;
using UnityEngine.EventSystems;

namespace KRPC.UI
{
    /// <summary>
    /// Tracks whether the pointer is holding down the control it is attached to, which
    /// is what lets a client repeat an action for as long as a button is held. The press
    /// has to be tracked in the game because it is driven by pointer events, which a
    /// client never sees.
    /// </summary>
    sealed class PressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        UnityEngine.UI.Selectable selectable;

        /// <summary>
        /// Whether the pointer is holding the control down.
        /// </summary>
        internal bool Pressed { get; private set; }

        public void Awake ()
        {
            selectable = GetComponent<UnityEngine.UI.Selectable> ();
        }

        /// <summary>
        /// The press starts, unless the control is not interactable and so ignores the
        /// user.
        /// </summary>
        public void OnPointerDown (PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable ())
                Pressed = true;
        }

        /// <summary>
        /// The press ends when the pointer is released, wherever it is by then: Unity
        /// sends the release to whatever took the press, even once the pointer has
        /// moved off it.
        /// </summary>
        public void OnPointerUp (PointerEventData eventData)
        {
            Pressed = false;
        }

        /// <summary>
        /// A control taken off the screen while held is no longer pressed; the release
        /// would never arrive.
        /// </summary>
        public void OnDisable ()
        {
            Pressed = false;
        }
    }
}
