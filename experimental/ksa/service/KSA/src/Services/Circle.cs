using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A circle. Created using <see cref="Drawing.AddCircle"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Circle : Drawable
    {
        double3 center;
        double3 normal;
        float radius;

        internal Circle (Tuple<double, double, double> center, Tuple<double, double, double> normal, float radius, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Center = center;
            Normal = normal;
            Radius = radius;
        }

        /// <summary>
        /// The position of the center of the circle, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Center {
            get { return Vectors.ToTuple (center); }
            set { center = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The direction normal to the plane of the circle, as a unit vector.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Normal {
            get { return Vectors.ToTuple (normal); }
            set { normal = Vectors.ToUnitVector (value, nameof (Normal)); }
        }

        /// <summary>
        /// The radius of the circle, in meters.
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
            var axisA = Vectors.Perpendicular (normal);
            var axisB = double3.Cross (normal, axisA);
            DrawArc (gizmos, camera, center, axisA * radius, axisB * radius, 2 * Math.PI);
        }
    }
}
