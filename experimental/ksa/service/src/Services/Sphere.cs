using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A solid sphere. Created using <see cref="Drawing.AddSphere"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Sphere : Drawable
    {
        double3 center;
        float radius;

        internal Sphere (Tuple<double, double, double> center, float radius, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// The position of the center of the sphere, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Center {
            get { return Vectors.ToTuple (center); }
            set { center = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The radius of the sphere, in meters.
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
            gizmos.DrawSphere (ToEgo (camera, center), radius, GizmoColor);
        }
    }
}
