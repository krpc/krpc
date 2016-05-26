using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.UI.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A polygon. Created using <see cref="Drawing.AddPolygon" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Polygon : Drawable
    {
        readonly LineRenderer renderer;
        IList<Vector3d> vertices;
        Tuple3 color;
        float thickness;

        internal Polygon (IList<Vector3d> vertices, ReferenceFrame referenceFrame, bool visible)
            : base ("polygon", typeof(LineRenderer))
        {
            renderer = GameObject.GetComponent<LineRenderer> ();
            renderer.useWorldSpace = true;
            renderer.SetVertexCount (vertices.Count + 1);
            for (int i = 0; i < vertices.Count + 1; i++)
                renderer.SetPosition (i, Vector3d.zero);
            this.vertices = vertices;
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new Tuple3 (1, 1, 1);
            Thickness = 0.1f;
        }

        /// <summary>
        /// Update the polygon.
        /// </summary>
        public override void Update ()
        {
            renderer.enabled = Visible;
            for (int i = 0; i < vertices.Count; i++)
                renderer.SetPosition (i, ReferenceFrame.PositionToWorldSpace (vertices [i]));
            renderer.SetPosition (vertices.Count, ReferenceFrame.PositionToWorldSpace (vertices [0]));
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

        /// <summary>
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return color; }
            set {
                color = value;
                var rgbColor = color.ToColor ();
                renderer.SetColors (rgbColor, rgbColor);
            }
        }

        /// <summary>
        /// Set the thickness
        /// </summary>
        [KRPCProperty]
        public float Thickness {
            get { return thickness; }
            set {
                thickness = value;
                renderer.SetWidth (thickness, thickness);
            }
        }
    }
}
