using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A polygon. Created using <see cref="Drawing.AddPolygon" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class Polygon : Drawable<Polygon>
    {
        readonly LineRenderer renderer;
        IList<Vector3d> vertices;
        Tuple3 color;
        float thickness;

        internal Polygon (IList<Vector3d> polygonVertices, ReferenceFrame referenceFrame, bool visible)
            : base (typeof(LineRenderer))
        {
            renderer = GameObject.GetComponent<LineRenderer> ();
            renderer.useWorldSpace = true;
            var numVertices = polygonVertices.Count + 1;
            renderer.SetVertexCount (numVertices);
            for (int i = 0; i < numVertices; i++)
                renderer.SetPosition (i, Vector3d.zero);
            vertices = polygonVertices;
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
            var numVertices = vertices.Count;
            for (int i = 0; i < numVertices; i++)
                renderer.SetPosition (i, ReferenceFrame.PositionToWorldSpace (vertices [i]));
            renderer.SetPosition (numVertices, ReferenceFrame.PositionToWorldSpace (vertices [0]));
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
        [SuppressMessage ("Gendarme.Rules.Design", "DoNotDeclareSettersOnCollectionPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
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
