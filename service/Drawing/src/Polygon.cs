using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A polygon. Created using <see cref="Drawing.AddPolygon" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Polygon : FramedPath<Polygon>
    {
        IList<Vector3d> vertices;

        internal Polygon (IList<Vector3d> polygonVertices, ReferenceFrame referenceFrame, bool visible)
        {
            vertices = polygonVertices;
            ReferenceFrame = referenceFrame;
            Visible = visible;
        }

        /// <summary>
        /// The vertices of the polygon, in world space.
        /// </summary>
        protected override IList<Vector3d> WorldPoints ()
        {
            return vertices.Select (ReferenceFrame.PositionToWorldSpace).ToList ();
        }

        /// <summary>
        /// Whether the polygon is closed, which it is.
        /// </summary>
        protected override bool Closed {
            get { return true; }
        }

        /// <summary>
        /// Destroy the drawable.
        /// </summary>
        public override void Destroy ()
        {
            vertices.Clear ();
            base.Destroy ();
        }

        /// <summary>
        /// Vertices for the polygon.
        /// </summary>
        [KRPCProperty]
        public IList<Tuple3> Vertices {
            get { return vertices.Select (x => x.ToTuple ()).ToList (); }
            set { vertices = value.Select (x => x.ToVector ()).ToList (); }
        }
    }
}
