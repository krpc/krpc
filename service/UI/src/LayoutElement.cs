using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
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
    public class LayoutElement : Equatable<LayoutElement>
    {
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
            return !ReferenceEquals (other, null) && element == other.element;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return element.GetHashCode ();
        }

        /// <summary>
        /// The smallest size the element can be given, in pixels. A negative value in either
        /// direction leaves that direction unconstrained.
        /// </summary>
        [KRPCProperty]
        public Tuple2 MinSize {
            get { return new Tuple2 (element.minWidth, element.minHeight); }
            set {
                var size = value.ToVector ();
                element.minWidth = size.x;
                element.minHeight = size.y;
            }
        }

        /// <summary>
        /// The size the element asks for, in pixels. A negative value in either direction
        /// leaves that direction unconstrained.
        /// </summary>
        [KRPCProperty]
        public Tuple2 PreferredSize {
            get { return new Tuple2 (element.preferredWidth, element.preferredHeight); }
            set {
                var size = value.ToVector ();
                element.preferredWidth = size.x;
                element.preferredHeight = size.y;
            }
        }

        /// <summary>
        /// The share the element takes of the space left over once every element has its
        /// preferred size. A negative value in either direction takes no share.
        /// </summary>
        [KRPCProperty]
        public Tuple2 FlexibleSize {
            get { return new Tuple2 (element.flexibleWidth, element.flexibleHeight); }
            set {
                var size = value.ToVector ();
                element.flexibleWidth = size.x;
                element.flexibleHeight = size.y;
            }
        }

        /// <summary>
        /// Whether the layout leaves the element out and lets it be positioned by hand.
        /// </summary>
        [KRPCProperty]
        public bool IgnoreLayout {
            get { return element.ignoreLayout; }
            set { element.ignoreLayout = value; }
        }
    }
}
