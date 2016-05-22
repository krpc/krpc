using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace KRPC.UI
{
    /// <summary>
    /// Component to make a panel draggable
    /// </summary>
    public class DragPanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        Vector2 pointerOffset;
        UnityEngine.RectTransform canvasRectTransform;
        UnityEngine.RectTransform panelRectTransform;

        void Awake ()
        {
            Canvas canvas = GetComponentInParent <Canvas> ();
            if (canvas != null) {
                canvasRectTransform = canvas.transform as UnityEngine.RectTransform;
                panelRectTransform = transform.parent as UnityEngine.RectTransform;
            }
        }

        /// <summary>
        /// Handle pointer down event.
        /// </summary>
        public void OnPointerDown (PointerEventData data)
        {
            panelRectTransform.SetAsLastSibling ();
            RectTransformUtility.ScreenPointToLocalPointInRectangle (panelRectTransform, data.position, data.pressEventCamera, out pointerOffset);
            Console.WriteLine ("DragPanel: pointer down = " + pointerOffset);
        }

        /// <summary>
        /// Handle drag event.
        /// </summary>
        public void OnDrag (PointerEventData data)
        {
            if (panelRectTransform == null)
                return;
            Vector2 pointerPostion = ClampToWindow (data);
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle (canvasRectTransform, pointerPostion, data.pressEventCamera, out localPointerPosition)) {
                panelRectTransform.anchoredPosition = localPointerPosition - pointerOffset;
                Console.WriteLine ("DragPanel: drag = " + panelRectTransform.anchoredPosition);
            } else
                Console.WriteLine ("DragPanel: no drag");
        }

        Vector2 ClampToWindow (PointerEventData data)
        {
            Vector2 rawPointerPosition = data.position;
            var canvasCorners = new Vector3[4];
            canvasRectTransform.GetWorldCorners (canvasCorners);
            float clampedX = Mathf.Clamp (rawPointerPosition.x, canvasCorners [0].x, canvasCorners [2].x);
            float clampedY = Mathf.Clamp (rawPointerPosition.y, canvasCorners [0].y, canvasCorners [2].y);
            return new Vector2 (clampedX, clampedY);
        }
    }
}
