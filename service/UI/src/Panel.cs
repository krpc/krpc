using System;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A container for user interface elements.
    /// Added to a <see cref="Canvas" /> or another panel.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Panel : Container
    {
        internal Panel (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.panel", 200, 200), visible)
        {
            Widgets.AddImage (GameObject, Widgets.Style (skin => skin.window));
        }

        /// <summary>
        /// Wrap a game object that is already part of another control, such as the
        /// contents of a scroll view, as a panel.
        /// </summary>
        internal Panel (GameObject obj)
            : base (obj, true, false)
        {
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
