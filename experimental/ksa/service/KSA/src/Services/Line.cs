using System;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A line. Created using <see cref="Drawing.AddLine"/> or <see cref="Drawing.AddDirection"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Line : Drawable
    {
        double3 start;
        double3 end;

        internal Line (Tuple<double, double, double> start, Tuple<double, double, double> end, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// The start position of the line, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Start {
            get { return Vectors.ToTuple (start); }
            set { start = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The end position of the line, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> End {
            get { return Vectors.ToTuple (end); }
            set { end = Vectors.ToVector (value); }
        }

        internal override void DrawGizmos (GizmosRenderer gizmos, global::KSA.Camera camera)
        {
            gizmos.DrawLine (ToEgo (camera, start), ToEgo (camera, end), GizmoColor);
        }
    }
}
