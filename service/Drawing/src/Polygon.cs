using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A polygon. Created using <see cref="Drawing.AddPolygon" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Polygon : Drawable<Polygon>
    {
        static readonly AnimationCurve constantWidth = AnimationCurve.Constant (0f, 1f, 1f);

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
            #pragma warning disable 618
            renderer.SetVertexCount (numVertices);
            #pragma warning restore
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
            if (!CanBeDrawn) {
                renderer.enabled = false;
                return;
            }
            var numVertices = vertices.Count;
            if (numVertices == 0) {
                renderer.enabled = false;
                return;
            }
            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            if (renderer.positionCount != numVertices + 1)
                renderer.positionCount = numVertices + 1;
            var positions = new Vector3 [numVertices + 1];
            for (int i = 0; i <= numVertices; i++)
                positions [i] = View.ToView (ReferenceFrame.PositionToWorldSpace (vertices [i % numVertices]));
            renderer.SetPositions (positions);
            renderer.widthCurve = View.Scaled ? WidthCurve (positions) : constantWidth;
            renderer.widthMultiplier = thickness;
        }

        // The width at each vertex, keyed by the fraction of the distance along the outline
        static AnimationCurve WidthCurve (Vector3[] positions)
        {
            var widths = new Keyframe [positions.Length];
            var length = 0f;
            for (int i = 0; i < positions.Length; i++) {
                if (i > 0)
                    length += Vector3.Distance (positions [i - 1], positions [i]);
                widths [i] = new Keyframe (length, View.SizeAt (positions [i]));
            }
            if (length > 0f) {
                for (int i = 0; i < widths.Length; i++)
                    widths [i].time /= length;
            }
            return new AnimationCurve (widths);
        }

        /// <summary>
        /// Destroy the drawable.
        /// </summary>
        public override void Destroy ()
        {
            vertices.Clear ();
            base.Destroy ();
        }

        // The line renderer, checked to still exist.
        LineRenderer LineRenderer {
            get {
                CheckExists ();
                return renderer;
            }
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
                #pragma warning disable 618
                LineRenderer.SetColors (rgbColor, rgbColor);
                #pragma warning restore
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
                #pragma warning disable 618
                LineRenderer.SetWidth (thickness, thickness);
                #pragma warning restore
            }
        }
    }
}
