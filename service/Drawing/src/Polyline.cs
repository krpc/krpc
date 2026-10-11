using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A line through a list of vertices, open at its ends. Created using
    /// <see cref="Drawing.AddPolyline" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Polyline : FramedPath<Polyline>
    {
        IList<Vector3d> vertices;

        internal Polyline (IList<Vector3d> polylineVertices, ReferenceFrame referenceFrame, bool visible)
        {
            vertices = polylineVertices;
            ReferenceFrame = referenceFrame;
            Visible = visible;
        }

        /// <summary>
        /// The vertices of the polyline, in world space.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            return vertices.Select (ReferenceFrame.PositionToWorldSpace).ToList ();
        }

        /// <summary>
        /// Whether the polyline is closed, which it is not.
        /// </summary>
        protected override bool Closed {
            get { return false; }
        }

        /// <summary>
        /// Vertices for the polyline.
        /// </summary>
        [KRPCProperty]
        public IList<Tuple3> Vertices {
            get { return vertices.Select (x => x.ToTuple ()).ToList (); }
            set { vertices = value.Select (x => x.ToVector ()).ToList (); }
        }
    }
}
