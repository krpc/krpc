using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.UI.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple2 = System.Tuple<double, double>;
using Tuple4Int = System.Tuple<int, int, int, int>;

namespace KRPC.UI
{
    /// <summary>
    /// Arranges the elements of a panel automatically, so that they do not have to be
    /// positioned one by one. See <see cref="Panel.AddHorizontalLayout" />,
    /// <see cref="Panel.AddVerticalLayout" /> and <see cref="Panel.AddGridLayout" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Layout : Equatable<Layout>
    {
        readonly UnityEngine.UI.LayoutGroup layout;

        internal Layout (UnityEngine.UI.LayoutGroup innerLayout)
        {
            layout = innerLayout;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Layout other)
        {
            return !ReferenceEquals (other, null) && layout == other.layout;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return layout.GetHashCode ();
        }

        UnityEngine.UI.GridLayoutGroup Grid {
            get {
                var grid = layout as UnityEngine.UI.GridLayoutGroup;
                if (grid == null)
                    throw new InvalidOperationException ("The layout is not a grid layout");
                return grid;
            }
        }

        UnityEngine.UI.HorizontalOrVerticalLayoutGroup Line {
            get {
                var line = layout as UnityEngine.UI.HorizontalOrVerticalLayoutGroup;
                if (line == null)
                    throw new InvalidOperationException (
                        "The layout is not a horizontal or vertical layout");
                return line;
            }
        }

        /// <summary>
        /// The gap left between the elements, in pixels. A grid layout uses it in both
        /// directions.
        /// </summary>
        [KRPCProperty]
        public float Spacing {
            get {
                var grid = layout as UnityEngine.UI.GridLayoutGroup;
                return grid != null ? grid.spacing.x : Line.spacing;
            }
            set {
                var grid = layout as UnityEngine.UI.GridLayoutGroup;
                if (grid != null)
                    grid.spacing = new Vector2 (value, value);
                else
                    Line.spacing = value;
            }
        }

        /// <summary>
        /// The gap left inside the edges of the panel, in whole pixels, as
        /// (left, right, top, bottom).
        /// </summary>
        [KRPCProperty]
        public Tuple4Int Padding {
            get {
                var padding = layout.padding;
                return new Tuple4Int (
                    padding.left, padding.right, padding.top, padding.bottom);
            }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (value));
                layout.padding = new RectOffset (
                    value.Item1, value.Item2, value.Item3, value.Item4);
            }
        }

        /// <summary>
        /// Where the elements are placed within the space the layout has to fill.
        /// </summary>
        [KRPCProperty]
        public TextAnchor ChildAlignment {
            get { return layout.childAlignment.ToTextAnchor (); }
            set { layout.childAlignment = value.FromTextAnchor (); }
        }

        /// <summary>
        /// The size of each cell of a grid layout, in pixels.
        /// </summary>
        /// <remarks>
        /// Only applies to a grid layout. Throws an exception for the other layouts, whose
        /// elements are sized individually using their layout elements.
        /// </remarks>
        [KRPCProperty]
        public Tuple2 CellSize {
            get { return new Tuple2 (Grid.cellSize.x, Grid.cellSize.y); }
            set { Grid.cellSize = value.ToVector (); }
        }

        /// <summary>
        /// What fixes the shape of a grid layout.
        /// </summary>
        /// <remarks>
        /// Only applies to a grid layout. Throws an exception for the other layouts.
        /// </remarks>
        [KRPCProperty]
        public GridConstraint Constraint {
            get { return Grid.constraint.ToGridConstraint (); }
            set { Grid.constraint = value.FromGridConstraint (); }
        }

        /// <summary>
        /// The number of columns or rows that <see cref="Constraint" /> fixes, ignored when
        /// the constraint is <see cref="GridConstraint.Flexible" />.
        /// </summary>
        /// <remarks>
        /// Only applies to a grid layout. Throws an exception for the other layouts.
        /// </remarks>
        [KRPCProperty]
        public int ConstraintCount {
            get { return Grid.constraintCount; }
            set {
                // Unity moves a count below one up to one, so a client that asked for
                // fewer would be left reading back a count it did not set and no way to
                // tell that it had happened.
                if (value < 1)
                    throw new ArgumentOutOfRangeException (
                        nameof (value), "A grid must have at least one column or row");
                Grid.constraintCount = value;
            }
        }
    }
}
