using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// An arc of a circle. Created using <see cref="Drawing.AddArc"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Arc : Drawable
    {
        double3 center;
        double3 normal;
        double3 startDirection;
        float radius;

        internal Arc (
            Tuple<double, double, double> center, Tuple<double, double, double> normal, Tuple<double, double, double> startDirection,
            float angle, float radius, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Center = center;
            Normal = normal;
            StartDirection = startDirection;
            Angle = angle;
            Radius = radius;
        }

        /// <summary>
        /// The position of the center of the arc's circle, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Center {
            get { return Vectors.ToTuple (center); }
            set { center = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The direction normal to the plane of the arc, as a unit vector.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Normal {
            get { return Vectors.ToTuple (normal); }
            set { normal = Vectors.ToUnitVector (value, nameof (Normal)); }
        }

        /// <summary>
        /// The direction from the center to the start of the arc, as a unit vector.
        /// </summary>
        /// <remarks>
        /// The part of the direction along <see cref="Normal"/> is ignored.
        /// </remarks>
        [KRPCProperty]
        public Tuple<double, double, double> StartDirection {
            get { return Vectors.ToTuple (startDirection); }
            set { startDirection = Vectors.ToUnitVector (value, nameof (StartDirection)); }
        }

        /// <summary>
        /// The angle the arc turns through, counterclockwise about <see cref="Normal"/>, in radians.
        /// </summary>
        [KRPCProperty]
        public float Angle { get; set; }

        /// <summary>
        /// The radius of the arc, in meters.
        /// </summary>
        [KRPCProperty]
        public float Radius {
            get { return radius; }
            set {
                if (!(value > 0))
                    throw new ArgumentOutOfRangeException (nameof (Radius));
                radius = value;
            }
        }

        internal override void DrawGizmos (GizmosRenderer gizmos, global::KSA.Camera camera)
        {
            var axisA = startDirection - normal * double3.Dot (startDirection, normal);
            if (axisA.LengthSquared () < 1e-12)
                return;
            axisA = axisA.Normalized ();
            var axisB = double3.Cross (normal, axisA);
            DrawArc (gizmos, camera, center, axisA * radius, axisB * radius, Angle);
        }
    }
}
