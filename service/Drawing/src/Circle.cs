using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A circle. Created using <see cref="Drawing.AddCircle" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Circle : FramedPath<Circle>
    {
        Vector3d center;
        Vector3d normal;
        double radius;

        internal Circle (Vector3d circleCenter, Vector3d circleNormal, double circleRadius,
                         ReferenceFrame referenceFrame, bool visible)
        {
            center = circleCenter;
            normal = circleNormal;
            radius = circleRadius;
            ReferenceFrame = referenceFrame;
            Visible = visible;
        }

        /// <summary>
        /// Raise if the normal is zero.
        /// </summary>
        internal static void CheckNormal (Vector3d normal)
        {
            if (normal.magnitude < 1e-9)
                throw new ArgumentException ("The normal must not be zero");
        }

        /// <summary>
        /// The points of the circle, in world space. The circle starts on the x axis of the
        /// reference frame, projected into its plane, or on the y axis when the normal is
        /// the x axis.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            var start = Vector3d.Exclude (normal, new Vector3d (1, 0, 0));
            if (start.magnitude < 1e-9)
                start = new Vector3d (0, 1, 0);
            var points = Arc.Points (center, normal, start, 2 * Math.PI, radius);
            // The path closes the circle back onto its first point
            if (points.Count > 0)
                points.RemoveAt (points.Count - 1);
            for (int i = 0; i < points.Count; i++)
                points [i] = ReferenceFrame.PositionToWorldSpace (points [i]);
            return points;
        }

        /// <summary>
        /// Whether the circle is closed, which it is.
        /// </summary>
        protected override bool Closed {
            get { return true; }
        }

        /// <summary>
        /// Position of the center of the circle.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Center {
            get { return center.ToTuple (); }
            set { center = value.ToVector (); }
        }

        /// <summary>
        /// Normal of the plane of the circle.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Normal {
            get { return normal.ToTuple (); }
            set {
                var newNormal = value.ToVector ();
                CheckNormal (newNormal);
                normal = newNormal;
            }
        }

        /// <summary>
        /// Radius of the circle, in meters.
        /// </summary>
        [KRPCProperty]
        public double Radius {
            get { return radius; }
            set { radius = value; }
        }
    }
}
