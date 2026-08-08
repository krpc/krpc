using UnityEngine;
using UnityEngine.EventSystems;

namespace KRPC.UI
{
    /// <summary>
    /// Shows a short piece of text beside the pointer while it rests on the control the
    /// handler is attached to. The tooltip is built when the pointer arrives and taken
    /// away when it leaves, so nothing hangs around for controls that are never pointed
    /// at, and nothing outlives its scene.
    /// </summary>
    sealed class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// How far the tooltip sits from the pointer, so that it reads beside the
        /// pointer's arrow rather than underneath it.
        /// </summary>
        static readonly Vector2 offset = new Vector2 (16, -16);

        GameObject tooltip;

        /// <summary>
        /// The text to show. Empty shows nothing.
        /// </summary>
        internal string Text { get; set; }

        public void OnPointerEnter (PointerEventData eventData)
        {
            Hide ();
            if (string.IsNullOrEmpty (Text))
                return;
            // The tooltip is parented to the canvas rather than the control, so that it
            // is drawn over the control's neighbors and is not cut off by the viewport
            // of a scroll view.
            var canvas = GetComponentInParent<UnityEngine.Canvas> ();
            if (canvas == null)
                return;
            tooltip = Widgets.CreateTooltip (canvas.rootCanvas.gameObject, Text);
            Place ();
        }

        public void OnPointerExit (PointerEventData eventData)
        {
            Hide ();
        }

        /// <summary>
        /// A control taken off the screen while pointed at gets no exit event, so its
        /// tooltip is taken away with it.
        /// </summary>
        public void OnDisable ()
        {
            Hide ();
        }

        public void OnDestroy ()
        {
            Hide ();
        }

        /// <summary>
        /// Follow the pointer while the tooltip is showing.
        /// </summary>
        public void Update ()
        {
            if (tooltip != null)
                Place ();
        }

        /// <summary>
        /// Put the tooltip beside the pointer. The pointer position is in screen pixels
        /// and the tooltip is placed in the canvas's own units, so the position is
        /// divided by however much the canvas is scaled by. The offset is already in
        /// canvas units, as the size of the tooltip is, so it is added afterwards and
        /// keeps the same distance from the pointer as the text it sits beside.
        /// </summary>
        void Place ()
        {
            var canvas = tooltip.GetComponentInParent<UnityEngine.Canvas> ();
            var scale = canvas == null || canvas.scaleFactor <= 0 ? 1 : canvas.scaleFactor;
            var rect = tooltip.GetComponent<UnityEngine.RectTransform> ();
            rect.anchoredPosition = (Vector2)Input.mousePosition / scale + offset;
        }

        void Hide ()
        {
            if (tooltip != null) {
                Destroy (tooltip);
                tooltip = null;
            }
        }
    }
}
