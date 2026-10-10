using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A solid sphere. Created using <see cref="Drawing.AddSphere" />.
    /// </summary>
    /// <remarks>
    /// The sphere is drawn with an additive material, so a darker color draws it more
    /// transparent.
    /// </remarks>
    [KRPCClass (Service = "Drawing")]
    public class Sphere : SphereShape<Sphere>
    {
        double radius;

        internal Sphere (Vector3d center, double sphereRadius, ReferenceFrame referenceFrame, bool visible)
            : base (center, referenceFrame, visible)
        {
            radius = sphereRadius;
        }

        /// <summary>
        /// The diameter of the sphere, from its radius in meters.
        /// </summary>
        protected override float Diameter (Vector3 viewPosition)
        {
            return 2f * View.ToViewLength (radius);
        }

        /// <summary>
        /// Position of the center of the sphere.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Center {
            get { return SpherePosition.ToTuple (); }
            set { SpherePosition = value.ToVector (); }
        }

        /// <summary>
        /// Radius of the sphere, in meters.
        /// </summary>
        [KRPCProperty]
        public double Radius {
            get { return radius; }
            set { radius = value; }
        }
    }
}
