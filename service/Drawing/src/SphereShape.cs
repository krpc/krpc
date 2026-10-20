using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for objects drawn as a sphere, at a position in a reference frame.
    /// </summary>
    public abstract class SphereShape<T> : Drawable<T>
    {
        static Mesh sphereMesh;

        readonly MeshRenderer renderer;
        // A copy of the sphere mesh, colored through its vertices as a line is
        readonly Mesh mesh;
        ColorRGB color;

        /// <summary>
        /// Create a sphere.
        /// </summary>
        protected SphereShape (Vector3d spherePosition, ReferenceFrame referenceFrame, bool visible)
            : base (typeof(MeshRenderer))
        {
            renderer = GameObject.GetComponent<MeshRenderer> ();
            mesh = UnityEngine.Object.Instantiate (SphereMesh);
            GameObject.AddComponent<MeshFilter> ().sharedMesh = mesh;
            SpherePosition = spherePosition;
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new ColorRGB (1, 1, 1);
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
        /// The position of the center of the sphere.
        /// </summary>
        protected Vector3d SpherePosition { get; set; }

        /// <summary>
        /// The diameter of the sphere at the given position in the space of the view.
        /// </summary>
        protected abstract float Diameter (Vector3 viewPosition);

        /// <summary>
        /// Whether the sphere can be drawn.
        /// </summary>
        protected override bool CanBeDrawn {
            get { return CanBeDrawnIn (ReferenceFrame); }
        }

        /// <summary>
        /// Update the sphere.
        /// </summary>
        public override void Update ()
        {
            if (!CanBeDrawn) {
                renderer.enabled = false;
                return;
            }
            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            var viewPosition = View.ToView (ReferenceFrame.PositionToWorldSpace (SpherePosition));
            GameObject.transform.position = viewPosition;
            GameObject.transform.localScale = Vector3.one * Diameter (viewPosition);
        }

        /// <summary>
        /// Destroy the sphere.
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
        /// The color of the object.
        /// </summary>
        [KRPCProperty]
        public ColorRGB Color {
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
    }
}
