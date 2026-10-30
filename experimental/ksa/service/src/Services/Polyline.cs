using System;
using System.Collections.Generic;
using System.Linq;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A line through a list of points. Created using <see cref="Drawing.AddPolyline"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class Polyline : Drawable
    {
        IList<double3> vertices;

        internal Polyline (IList<Tuple<double, double, double>> vertices, bool closed, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Vertices = vertices;
            Closed = closed;
        }

        /// <summary>
        /// The points the line passes through, in meters.
        /// </summary>
        [KRPCProperty]
        public IList<Tuple<double, double, double>> Vertices {
            get { return vertices.Select (Vectors.ToTuple).ToList (); }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (value));
                vertices = value.Select (Vectors.ToVector).ToList ();
            }
        }

        /// <summary>
        /// Whether the line joins the last point back to the first.
        /// </summary>
        [KRPCProperty]
        public bool Closed { get; set; }

        internal override void DrawGizmos (GizmosRenderer gizmos, global::KSA.Camera camera)
        {
            var color = GizmoColor;
            var count = Closed && vertices.Count > 2 ? vertices.Count : vertices.Count - 1;
            for (int i = 0; i < count; i++)
                gizmos.DrawLine (ToEgo (camera, vertices [i]), ToEgo (camera, vertices [(i + 1) % vertices.Count]), color);
        }
    }
}
