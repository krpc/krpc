using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The edges of a box. Created using <see cref="Drawing.AddBox"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Box : Drawable
    {
        double3 center;
        double3 size;
        doubleQuat rotation;

        internal Box (Tuple<double, double, double> center, Tuple<double, double, double> size, Tuple<double, double, double, double> rotation, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Center = center;
            Size = size;
            Rotation = rotation;
        }

        /// <summary>
        /// The position of the center of the box, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Center {
            get { return Vectors.ToTuple (center); }
            set { center = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The lengths of the box's edges along its x, y and z axes, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Size {
            get { return Vectors.ToTuple (size); }
            set { size = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The rotation of the box's axes, as a quaternion of the form <c>(x, y, z, w)</c>.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double, double> Rotation {
            get { return Vectors.ToTuple (rotation); }
            set { rotation = Vectors.ToRotation (value); }
        }

        internal override void DrawGizmos (GizmosRenderer gizmos, global::KSA.Camera camera)
        {
            var halfX = DirectionToEgo (double3.UnitX.Transform (rotation)) * (size.X / 2);
            var halfY = DirectionToEgo (double3.UnitY.Transform (rotation)) * (size.Y / 2);
            var halfZ = DirectionToEgo (double3.UnitZ.Transform (rotation)) * (size.Z / 2);
            gizmos.DrawWireBox (ToEgo (camera, center), halfX, halfY, halfZ, GizmoColor);
        }
    }
}
