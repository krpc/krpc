using System;
using System.Runtime.CompilerServices;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple2 = System.Tuple<double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// How much space a user interface element asks a <see cref="Layout" /> for.
    /// </summary>
    /// <remarks>
    /// A layout gives every element its minimum size first. Any space left over is shared
    /// out to bring elements up to their preferred size, and anything still left over is
    /// shared between the elements with a flexible size, in proportion to it.
    /// </remarks>
    [KRPCClass (Service = "UI")]
    public class LayoutElement : Equatable<LayoutElement>, IGameObjectState
    {
        // The game's layout element, which is what this stands for: it belongs to the
        // interface element it was taken from and lives exactly as long as that element,
        // and the game gives it nothing else to be named by.
        readonly UnityEngine.UI.LayoutElement element;

        internal LayoutElement (UnityEngine.UI.LayoutElement innerElement)
        {
            element = innerElement;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (LayoutElement other)
        {
            return !ReferenceEquals (other, null) &&
            ReferenceEquals (element, other.element);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // The element's identity hash rather than its own, so that nothing the game
            // does to it can change it while a client holds this object.
            return RuntimeHelpers.GetHashCode (element);
        }

        /// <summary>
        /// The state of the layout element. It belongs to the interface element it was
        /// taken from, and the game destroys it with that element. The layout element is
        /// never built again.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return element == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        // The game's layout element, checked to still exist.
        UnityEngine.UI.LayoutElement Internal {
            get {
                if (GameObjectState == GameObjectState.Destroyed)
                    throw new ObjectDestroyedException (
                        "The layout element no longer exists, as the user interface object " +
                        "it belongs to has been removed.");
                return element;
            }
        }

        /// <summary>
        /// The smallest size the element can be given, in pixels. A negative value in either
        /// direction leaves that direction unconstrained.
        /// </summary>
        [KRPCProperty]
        public Tuple2 MinSize {
            get { return new Tuple2 (Internal.minWidth, Internal.minHeight); }
            set {
                var size = value.ToVector ();
                Internal.minWidth = size.x;
                Internal.minHeight = size.y;
            }
        }

        /// <summary>
        /// The size the element asks for, in pixels. A negative value in either direction
        /// leaves that direction unconstrained.
        /// </summary>
        [KRPCProperty]
        public Tuple2 PreferredSize {
            get { return new Tuple2 (Internal.preferredWidth, Internal.preferredHeight); }
            set {
                var size = value.ToVector ();
                Internal.preferredWidth = size.x;
                Internal.preferredHeight = size.y;
            }
        }

        /// <summary>
        /// The share the element takes of the space left over once every element has its
        /// preferred size. A negative value in either direction takes no share.
        /// </summary>
        [KRPCProperty]
        public Tuple2 FlexibleSize {
            get { return new Tuple2 (Internal.flexibleWidth, Internal.flexibleHeight); }
            set {
                var size = value.ToVector ();
                Internal.flexibleWidth = size.x;
                Internal.flexibleHeight = size.y;
            }
        }

        /// <summary>
        /// Whether the layout leaves the element out and lets it be positioned by hand.
        /// </summary>
        [KRPCProperty]
        public bool IgnoreLayout {
            get { return Internal.ignoreLayout; }
            set { Internal.ignoreLayout = value; }
        }
    }
}
