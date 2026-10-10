using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A point, drawn as a dot that keeps its size on screen. Created using
    /// <see cref="Drawing.AddPoint" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Point : Drawable<Point>
    {
        static Mesh sphereMesh;

        readonly MeshRenderer renderer;
        // A copy of the sphere mesh, colored through its vertices as a line is
        readonly Mesh mesh;
        Vector3d position;
        Tuple3 color;
        float size;

        internal Point (Vector3d pointPosition, ReferenceFrame referenceFrame, bool visible)
            : base (typeof(MeshRenderer))
        {
            renderer = GameObject.GetComponent<MeshRenderer> ();
            mesh = UnityEngine.Object.Instantiate (SphereMesh);
            GameObject.AddComponent<MeshFilter> ().sharedMesh = mesh;
            position = pointPosition;
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new Tuple3 (1, 1, 1);
            Size = 8f;
        }

        // The mesh of the game's built in sphere, which has a diameter of one
        static Mesh SphereMesh {
            get {
                if (sphereMesh == null) {
                    var sphere = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                    sphereMesh = sphere.GetComponent<MeshFilter> ().sharedMesh;
                    UnityEngine.Object.DestroyImmediate (sphere);
                }
                return sphereMesh;
            }
        }

        /// <summary>
        /// Whether the point can be drawn.
        /// </summary>
        protected override bool CanBeDrawn {
            get { return CanBeDrawnIn (ReferenceFrame); }
        }

        /// <summary>
        /// Update the point.
        /// </summary>
        public override void Update ()
        {
            if (!CanBeDrawn) {
                renderer.enabled = false;
                return;
            }
            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            var viewPosition = View.ToView (ReferenceFrame.PositionToWorldSpace (position));
            GameObject.transform.position = viewPosition;
            GameObject.transform.localScale = Vector3.one * size * View.PixelSize (viewPosition);
        }

        /// <summary>
        /// Destroy the point.
        /// </summary>
        public override void Destroy ()
        {
            UnityEngine.Object.Destroy (mesh);
            base.Destroy ();
        }

        /// <summary>
        /// Reference frame for the positions of the object.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Position of the point.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Position {
            get { return position.ToTuple (); }
            set { position = value.ToVector (); }
        }

        /// <summary>
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return color; }
            set {
                CheckExists ();
                color = value;
                var rgbColor = color.ToColor ();
                var colors = new UnityEngine.Color [mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                    colors [i] = rgbColor;
                mesh.colors = colors;
            }
        }

        /// <summary>
        /// Diameter of the point, in pixels.
        /// </summary>
        [KRPCProperty]
        public float Size {
            get { return size; }
            set { size = value; }
        }
    }
}
