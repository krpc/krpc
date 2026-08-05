using System;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A container for user interface elements.
    /// Added to a <see cref="Canvas" /> or another panel.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Panel : Container
    {
        PanelStyle style;

        internal Panel (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.panel", 200, 200), visible)
        {
            Style = PanelStyle.Window;
        }

        /// <summary>
        /// Wrap a game object that is already part of another control, such as the
        /// contents of a scroll view, as a panel.
        /// </summary>
        internal Panel (GameObject obj)
            : base (obj, true, false)
        {
            style = PanelStyle.None;
        }

        /// <summary>
        /// The image the background of the panel is drawn with, added on first use so that
        /// a panel which is only ever a container costs nothing to draw.
        /// </summary>
        UnityEngine.UI.Image Background {
            get {
                var image = GameObject.GetComponent<UnityEngine.UI.Image> ();
                if (image == null) {
                    image = Widgets.AddImage (GameObject, null);
                    // It has no sprite yet, and Unity draws an image that has no sprite
                    // as a rectangle filled with its color, so it is left undrawn until
                    // a style gives it one.
                    image.enabled = false;
                }
                return image;
            }
        }

        /// <summary>
        /// How the background of the panel is drawn. A box is the frame to group related
        /// controls in, and is how a group box is made.
        /// </summary>
        [KRPCProperty]
        public PanelStyle Style {
            get { return style; }
            set {
                var background = Background;
                var newStyle = value == PanelStyle.Box
                    ? Widgets.Style (skin => skin.box)
                    : value == PanelStyle.Window ? Widgets.Style (skin => skin.window) : null;
                var sprite = newStyle == null || newStyle.normal == null
                    ? null : newStyle.normal.background;
                background.sprite = sprite;
                if (sprite != null)
                    background.type = UnityEngine.UI.Image.Type.Sliced;
                // Unity draws an image that has no sprite as a rectangle filled with its
                // color, so a panel is stopped from being drawn rather than left to fill
                // itself in: one with no style, and one whose style the game gave no
                // sprite for, which falls back to not being drawn rather than to a solid
                // block hiding whatever is behind it.
                background.enabled = sprite != null;
                style = value;
            }
        }

        /// <summary>
        /// The color the background of the panel is tinted with, as
        /// (red, green, blue, alpha). An alpha of 0 is fully transparent and 1 is fully
        /// opaque.
        /// </summary>
        /// <remarks>
        /// Has no effect while the style of the panel is <see cref="PanelStyle.None" />,
        /// as the panel is then not drawn at all.
        /// </remarks>
        [KRPCProperty]
        public Tuple4 Color {
            get { return Background.color.ToRgbaTuple (); }
            set { Background.color = value.ToColor (); }
        }

        /// <summary>
        /// Whether the user can move the panel by dragging it.
        /// </summary>
        /// <remarks>
        /// Dragging anywhere on the panel moves it, including over the elements it
        /// contains, unless one of them takes the pointer for itself. Give a panel a title
        /// bar of its own if only part of it should move it.
        /// A panel whose style is <see cref="PanelStyle.None" /> is not drawn, so it takes
        /// no pointer events of its own and can only be dragged by the elements it
        /// contains.
        /// </remarks>
        [KRPCProperty]
        public bool Draggable {
            get {
                var handler = GameObject.GetComponent<DragHandler> ();
                return handler != null && handler.enabled;
            }
            set {
                var handler = GameObject.GetComponent<DragHandler> ();
                if (handler == null) {
                    if (!value)
                        return;
                    handler = GameObject.AddComponent<DragHandler> ();
                }
                handler.enabled = value;
            }
        }

        /// <summary>
        /// The layout that arranges the elements of the panel, or <c>null</c> if they are
        /// positioned by hand.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Layout Layout {
            get {
                var layout = GameObject.GetComponent<UnityEngine.UI.LayoutGroup> ();
                return layout == null ? null : new Layout (layout);
            }
        }

        /// <summary>
        /// Arrange the elements of the panel in a row.
        /// </summary>
        [KRPCMethod]
        public Layout AddHorizontalLayout ()
        {
            return AddLayout<UnityEngine.UI.HorizontalLayoutGroup> ();
        }

        /// <summary>
        /// Arrange the elements of the panel in a column.
        /// </summary>
        [KRPCMethod]
        public Layout AddVerticalLayout ()
        {
            return AddLayout<UnityEngine.UI.VerticalLayoutGroup> ();
        }

        /// <summary>
        /// Arrange the elements of the panel in a grid of equally sized cells.
        /// </summary>
        /// <remarks>
        /// The number of columns follows from the size of the panel and the size of a cell.
        /// Use <see cref="Layout.Constraint" /> to fix it instead.
        /// </remarks>
        [KRPCMethod]
        public Layout AddGridLayout ()
        {
            return AddLayout<UnityEngine.UI.GridLayoutGroup> ();
        }

        Layout AddLayout<T> () where T : UnityEngine.UI.LayoutGroup
        {
            // Unity applies every layout group on an object, and two of them fight over
            // where the elements go, so a panel is allowed only one.
            if (GameObject.GetComponent<UnityEngine.UI.LayoutGroup> () != null)
                throw new InvalidOperationException ("The panel already has a layout");
            return new Layout (GameObject.AddComponent<T> ());
        }

        /// <summary>
        /// Sizes the panel to fit what it contains.
        /// </summary>
        /// <remarks>
        /// Both directions start out unconstrained, which leaves the size of the panel
        /// alone.
        /// </remarks>
        [KRPCProperty]
        public SizeFitter SizeFitter {
            get {
                var fitter = GameObject.GetComponent<UnityEngine.UI.ContentSizeFitter> ();
                if (fitter == null)
                    fitter = GameObject.AddComponent<UnityEngine.UI.ContentSizeFitter> ();
                return new SizeFitter (fitter);
            }
        }
    }
}
