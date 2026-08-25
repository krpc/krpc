using System;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple2 = System.Tuple<double, double>;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// A Unity engine Rect Transform for a UI object.
    /// See the <a href="https://docs.unity3d.com/Manual/class-RectTransform.html">Unity manual</a> for more details.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class RectTransform : Equatable<RectTransform>, IGameObjectState
    {
        // The game's rect transform. It belongs to the interface element it was taken from and
        // lives exactly as long as that element, and the game gives it no other name
        readonly UnityEngine.RectTransform rectTransform;

        internal RectTransform (UnityEngine.RectTransform innerRectTransform)
        {
            if (ReferenceEquals (innerRectTransform, null))
                throw new ArgumentNullException (nameof (innerRectTransform));
            rectTransform = innerRectTransform;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (RectTransform other)
        {
            return !ReferenceEquals (other, null) &&
            ReferenceEquals (rectTransform, other.rectTransform);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The transform's identity hash, so that nothing the game does to it changes it
            // while a client holds this object
            return RuntimeHelpers.GetHashCode (rectTransform);
        }

        /// <summary>
        /// The state of the rect transform. It belongs to the interface element it was
        /// taken from, and the game destroys it with that element. The rect transform is
        /// never built again.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return rectTransform == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        // The game's rect transform, checked to still exist.
        UnityEngine.RectTransform Internal {
            get {
                if (GameObjectState == GameObjectState.Destroyed)
                    throw new ObjectDestroyedException (
                        "The rect transform no longer exists, as the user interface object " +
                        "it belongs to has been removed.");
                return rectTransform;
            }
        }

        /// <summary>
        /// Position of the rectangles pivot point relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Position {
            get { return Internal.anchoredPosition.ToTuple (); }
            set { Internal.anchoredPosition = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles pivot point relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple3 LocalPosition {
            get { return Internal.localPosition.ToTuple (); }
            set { Internal.localPosition = value.ToVector (); }
        }

        /// <summary>
        /// Width and height of the rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Size {
            get { return Internal.sizeDelta.ToTuple (); }
            set { Internal.sizeDelta = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles upper right corner relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 UpperRight {
            get { return Internal.offsetMax.ToTuple (); }
            set { Internal.offsetMax = value.ToVector (); }
        }

        /// <summary>
        /// Position of the rectangles lower left corner relative to the anchors.
        /// </summary>
        [KRPCProperty]
        public Tuple2 LowerLeft {
            get { return Internal.offsetMin.ToTuple (); }
            set { Internal.offsetMin = value.ToVector (); }
        }

        /// <summary>
        /// Set the minimum and maximum anchor points as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Anchor {
            set {
                Internal.anchorMax = value.ToVector ();
                Internal.anchorMin = value.ToVector ();
            }
        }

        /// <summary>
        /// The anchor point for the lower left corner of the rectangle defined as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 AnchorMax {
            get { return Internal.anchorMax.ToTuple (); }
            set { Internal.anchorMax = value.ToVector (); }
        }

        /// <summary>
        /// The anchor point for the upper right corner of the rectangle defined as a fraction of the size of the parent rectangle.
        /// </summary>
        [KRPCProperty]
        public Tuple2 AnchorMin {
            get { return Internal.anchorMin.ToTuple (); }
            set { Internal.anchorMin = value.ToVector (); }
        }

        /// <summary>
        /// Location of the pivot point around which the rectangle rotates, defined as a fraction of the size of the rectangle itself.
        /// </summary>
        [KRPCProperty]
        public Tuple2 Pivot {
            get { return Internal.pivot.ToTuple (); }
            set { Internal.pivot = value.ToVector (); }
        }

        /// <summary>
        /// Rotation, as a quaternion, of the object around its pivot point.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return Internal.localRotation.ToTuple (); }
            set { Internal.localRotation = value.ToQuaternion (); }
        }

        /// <summary>
        /// Scale factor applied to the object in the x, y and z dimensions.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Scale {
            get { return Internal.localScale.ToTuple (); }
            set { Internal.localScale = value.ToVector (); }
        }
    }
}
