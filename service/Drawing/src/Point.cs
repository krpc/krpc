using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A point, drawn as a dot that keeps its size on screen. Created using
    /// <see cref="Drawing.AddPoint" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Point : SphereShape<Point>
    {
        float size = 8f;

        internal Point (Vector3d pointPosition, ReferenceFrame referenceFrame, bool visible)
            : base (pointPosition, referenceFrame, visible)
        {
        }

        /// <summary>
        /// The diameter of the point, from its size in pixels.
        /// </summary>
        protected override float Diameter (Vector3 viewPosition)
        {
            return size * View.PixelSize (viewPosition);
        }

        /// <summary>
        /// Position of the point.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Position {
            get { return SpherePosition.ToTuple (); }
            set { SpherePosition = value.ToVector (); }
        }

        /// <summary>
        /// Diameter of the point, in pixels.
        /// </summary>
        [KRPCProperty]
        public float Size {
            get { return size; }
            set { size = value; }
        }
    }
}
