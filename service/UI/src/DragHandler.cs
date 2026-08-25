using UnityEngine;
using UnityEngine.EventSystems;

namespace KRPC.UI
{
    /// <summary>
    /// Moves the object it is attached to when the user drags it, so that a panel can be
    /// used as a window. Dragging has to be handled in the game because it is driven by
    /// pointer events, which a client never sees.
    /// </summary>
    sealed class DragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        /// <summary>
        /// Bring the panel in front of its siblings, the way a window comes to the
        /// front when it is clicked. Unity hands the press to the topmost element under
        /// the pointer that takes one, so pressing a control inside the panel does not
        /// reach this; pressing the panel itself does.
        /// </summary>
        public void OnPointerDown (PointerEventData eventData)
        {
            transform.SetAsLastSibling ();
        }

        /// <summary>
        /// Move the panel by however far the pointer moved.
        /// </summary>
        public void OnDrag (PointerEventData eventData)
        {
            if (eventData == null)
                return;
            var rect = GetComponent<UnityEngine.RectTransform> ();
            if (rect == null)
                return;
            // The pointer moves in screen pixels and the panel is positioned in the canvas's own
            // units, so the movement is divided by the canvas scale. Otherwise the panel runs
            // away from the pointer on a scaled interface
            var canvas = GetComponentInParent<UnityEngine.Canvas> ();
            var scale = canvas == null || canvas.scaleFactor <= 0 ? 1 : canvas.scaleFactor;
            rect.anchoredPosition += eventData.delta / scale;
        }
    }
}
