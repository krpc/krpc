using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using Tuple2 = KRPC.Utils.Tuple<double, double>;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A Unity engine Rect Transform for a UI object.
    /// See the <a href="http://docs.unity3d.com/Manual/class-RectTransform.html">Unity manual</a> for more details.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class RectTransform
    {
        readonly UnityEngine.RectTransform rectTransform;

        internal RectTransform (UnityEngine.RectTransform innerRectTransform)
        {
            rectTransform = innerRectTransform;
        }

        /// <summary>
        /// Position of the rectangles pivot point relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Position {
            get { return rectTransform.anchoredPosition.ToTuple (); }
            set { rectTransform.anchoredPosition = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles pivot point relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple3 LocalPosition {
            get { return rectTransform.localPosition.ToTuple (); }
            set { rectTransform.localPosition = value.ToVector (); }
        }

        /// <summary>
        /// Width and height of the rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Size {
            get { return rectTransform.sizeDelta.ToTuple (); }
            set { rectTransform.sizeDelta = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles upper right corner relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 UpperRight {
            get { return rectTransform.offsetMax.ToTuple (); }
            set { rectTransform.offsetMax = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles lower left corner relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 LowerLeft {
            get { return rectTransform.offsetMin.ToTuple (); }
            set { rectTransform.offsetMin = value.ToVector (); }
        }

        /// <summary>
        /// Set the minimum and maximum anchor points as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        [SuppressMessage ("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Tuple2 Anchor {
            set {
                rectTransform.anchorMax = value.ToVector ();
                rectTransform.anchorMin = value.ToVector ();
            }
        }

        /// <summary>
        /// The anchor point for the lower left corner of the rectangle defined as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 AnchorMax {
            get { return rectTransform.anchorMax.ToTuple (); }
            set { rectTransform.anchorMax = value.ToVector (); }
        }

        /// <summary>
        /// The anchor point for the upper right corner of the rectangle defined as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 AnchorMin {
            get { return rectTransform.anchorMin.ToTuple (); }
            set { rectTransform.anchorMin = value.ToVector (); }
        }

        /// <summary>
        /// Location of the pivot point around which the rectangle rotates, defined as a fraction of the size of the rectangle itself.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Pivot {
            get { return rectTransform.pivot.ToTuple (); }
            set { rectTransform.pivot = value.ToVector (); }
        }

        /// <summary>
        /// Rotation, as a quaternion, of the object around its pivot point.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return rectTransform.localRotation.ToTuple (); }
            set { rectTransform.localRotation = value.ToQuaternion (); }
        }

        /// <summary>
        /// Scale factor applied to the object in the x, y and z dimensions.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Scale {
            get { return rectTransform.localScale.ToTuple (); }
            set { rectTransform.localScale = value.ToVector (); }
        }
    }
}
