using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// An arc of a circle. Created using <see cref="Drawing.AddArc" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Arc : FramedPath<Arc>
    {
        // The number of segments in a full turn
        const int segmentsPerTurn = 64;
        const int minSegments = 8;

        Vector3d center;
        Vector3d normal;
        Vector3d startDirection;
        double angle;
        double radius;

        internal Arc (Vector3d arcCenter, Vector3d arcNormal, Vector3d arcStartDirection, double arcAngle,
                      double arcRadius, ReferenceFrame referenceFrame, bool visible)
        {
            center = arcCenter;
            normal = arcNormal;
            startDirection = arcStartDirection;
            angle = arcAngle;
            radius = arcRadius;
            ReferenceFrame = referenceFrame;
            Visible = visible;
        }

        /// <summary>
        /// Raise if the normal and start direction do not define a plane.
        /// </summary>
        internal static void CheckPlane (Vector3d normal, Vector3d startDirection)
        {
            if (normal.magnitude < 1e-9)
                throw new ArgumentException ("The normal must not be zero");
            if (Vector3d.Exclude (normal, startDirection).magnitude < 1e-9)
                throw new ArgumentException ("The start direction must not be parallel to the normal");
        }

        /// <summary>
        /// The points of an arc in the plane with the given normal, from the start direction
        /// and turning right-handed about the normal. Empty if the plane is not defined.
        /// </summary>
        internal static IList<Vector3d> Points (Vector3d center, Vector3d normal, Vector3d startDirection,
                                                double angle, double radius)
        {
            var points = new List<Vector3d> ();
            var n = normal.normalized;
            var u = Vector3d.Exclude (n, startDirection);
            if (n.magnitude < 1e-9 || u.magnitude < 1e-9)
                return points;
            u = u.normalized;
            var v = Vector3d.Cross (n, u);
            // More than a full turn retraces the circle
            angle = Math.Max (-2 * Math.PI, Math.Min (2 * Math.PI, angle));
            var segments = Math.Max (minSegments, (int)Math.Ceiling (segmentsPerTurn * Math.Abs (angle) / (2 * Math.PI)));
            for (int i = 0; i <= segments; i++) {
                var theta = angle * i / segments;
                points.Add (center + radius * (Math.Cos (theta) * u + Math.Sin (theta) * v));
            }
            return points;
        }

        /// <summary>
        /// The points of the arc, in world space.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            var points = Points (center, normal, startDirection, angle, radius);
            for (int i = 0; i < points.Count; i++)
                points [i] = ReferenceFrame.PositionToWorldSpace (points [i]);
            return points;
        }

        /// <summary>
        /// Whether the arc is closed, which it is not.
        /// </summary>
        protected override bool Closed {
            get { return false; }
        }

        /// <summary>
        /// Position of the center of the arc.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Center {
            get { return center.ToTuple (); }
            set { center = value.ToVector (); }
        }

        /// <summary>
        /// Normal of the plane of the arc.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Normal {
            get { return normal.ToTuple (); }
            set {
                var newNormal = value.ToVector ();
                CheckPlane (newNormal, startDirection);
                normal = newNormal;
            }
        }

        /// <summary>
        /// Direction from the center to the start of the arc.
        /// </summary>
        [KRPCProperty]
        public Tuple3 StartDirection {
            get { return startDirection.ToTuple (); }
            set {
                var newStartDirection = value.ToVector ();
                CheckPlane (normal, newStartDirection);
                startDirection = newStartDirection;
            }
        }

        /// <summary>
        /// Angle of the arc, in radians.
        /// </summary>
        [KRPCProperty]
        public double Angle {
            get { return angle; }
            set { angle = value; }
        }

        /// <summary>
        /// Radius of the arc, in meters.
        /// </summary>
        [KRPCProperty]
        public double Radius {
            get { return radius; }
            set { radius = value; }
        }
    }
}
