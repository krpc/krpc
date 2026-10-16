using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// The gap left inside each edge of a layout, in whole pixels.
    /// </summary>
    /// <remarks>
    /// See <see cref="Layout.Padding"/>.
    /// </remarks>
    [KRPCStruct (Service = "UI")]
    public struct Padding
    {
        internal Padding (int left, int right, int top, int bottom) : this ()
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        internal Padding (RectOffset offset) : this (offset.left, offset.right, offset.top, offset.bottom)
        {
        }

        internal RectOffset ToRectOffset ()
        {
            return new RectOffset (Left, Right, Top, Bottom);
        }

        /// <summary>
        /// The gap inside the left edge.
        /// </summary>
        [KRPCProperty]
        public int Left { get; set; }

        /// <summary>
        /// The gap inside the right edge.
        /// </summary>
        [KRPCProperty]
        public int Right { get; set; }

        /// <summary>
        /// The gap inside the top edge.
        /// </summary>
        [KRPCProperty]
        public int Top { get; set; }

        /// <summary>
        /// The gap inside the bottom edge.
        /// </summary>
        [KRPCProperty]
        public int Bottom { get; set; }
    }
}
