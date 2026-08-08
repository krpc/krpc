using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A view onto a panel that is larger than the space available for it, with scroll bars
    /// to move around it.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    /// <remarks>
    /// Add the elements to <see cref="Content" />, not to the scroll view itself. Giving
    /// the content a layout and setting its size fitter to the preferred size is the
    /// simplest way to have it grow to hold what is put in it.
    /// </remarks>
    [KRPCClass (Service = "UI")]
    public class ScrollView : Object
    {
        /// <summary>
        /// How wide a scroll bar is drawn, in pixels.
        /// </summary>
        const float scrollbarThickness = 16;

        readonly UnityEngine.UI.ScrollRect scrollRect;
        readonly Panel content;

        internal ScrollView (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.scrollView", 200, 200), visible)
        {
            Widgets.AddImage (GameObject, Widgets.Style (skin => skin.scrollView));

            var viewport = Widgets.CreateFilling (GameObject, "krpc.scrollView.viewport", 0);
            // Anything outside the viewport is cut off rather than drawn over the rest of
            // the interface.
            viewport.AddComponent<UnityEngine.UI.RectMask2D> ();

            var contentObject = Widgets.CreateTopLeft (
                viewport, "krpc.scrollView.content", 200, 200);
            content = new Panel (contentObject);

            scrollRect = GameObject.AddComponent<UnityEngine.UI.ScrollRect> ();
            scrollRect.viewport = viewport.GetComponent<UnityEngine.RectTransform> ();
            scrollRect.content = contentObject.GetComponent<UnityEngine.RectTransform> ();
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar =
                Widgets.CreateScrollbar (GameObject, true, scrollbarThickness);
            scrollRect.horizontalScrollbar =
                Widgets.CreateScrollbar (GameObject, false, scrollbarThickness);
            // Unity hides a scroll bar that is not needed and gives the space back to the
            // viewport, so the view is not left with a gap along its edge.
            scrollRect.verticalScrollbarVisibility =
                UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.horizontalScrollbarVisibility =
                UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        }

        /// <summary>
        /// The panel that is scrolled, which the elements to show are added to.
        /// </summary>
        [KRPCProperty]
        public Panel Content {
            get { return content; }
        }

        /// <summary>
        /// Whether the view can be scrolled from side to side.
        /// </summary>
        [KRPCProperty]
        public bool Horizontal {
            get { return scrollRect.horizontal; }
            set { scrollRect.horizontal = value; }
        }

        /// <summary>
        /// Whether the view can be scrolled up and down.
        /// </summary>
        [KRPCProperty]
        public bool Vertical {
            get { return scrollRect.vertical; }
            set { scrollRect.vertical = value; }
        }
    }
}
