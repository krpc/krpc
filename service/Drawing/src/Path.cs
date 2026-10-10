using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// Abstract base class for objects drawn as a line through a list of points.
    /// </summary>
    public abstract class Path<T> : Drawable<T>
    {
        static readonly AnimationCurve constantWidth = AnimationCurve.Constant (0f, 1f, 1f);
        // The width of an arrow head at its base, as a multiple of the thickness of the path.
        // The head is twice as long as it is wide.
        const float arrowWidth = 4f;
        // The largest number of keys a Unity gradient holds
        const int maxColors = 8;
        // The length of a dash, and of the gap after it, as a multiple of the thickness
        const float dashLength = 4f;
        // The turn in degrees at a vertex past which it is a corner, where the dashes restart
        const float cornerAngle = 30f;
        // The most dash and gap pieces drawn between two corners
        const int maxDashes = 4096;

        static Texture2D dashTexture;

        readonly LineRenderer renderer;
        readonly LineRenderer startHead;
        readonly LineRenderer endHead;
        readonly TextMesh label;
        readonly MeshRenderer labelRenderer;
        readonly List<Vector3> points = new List<Vector3> ();
        Material headMaterial;
        string labelText = string.Empty;
        IList<Tuple3> colors;
        float thickness;
        float arrowSize = 1f;

        /// <summary>
        /// Create a path.
        /// </summary>
        protected Path ()
            : base (typeof(LineRenderer))
        {
            renderer = GameObject.GetComponent<LineRenderer> ();
            renderer.useWorldSpace = true;
            renderer.positionCount = 0;
            startHead = CreateHead ("StartArrow");
            endHead = CreateHead ("EndArrow");
            var labelObject = new GameObject (GameObject.name + ".Label");
            labelObject.transform.SetParent (GameObject.transform, false);
            label = labelObject.AddComponent<TextMesh> ();
            label.text = string.Empty;
            label.fontSize = 12;
            label.anchor = TextAnchor.LowerCenter;
            labelRenderer = labelObject.GetComponent<MeshRenderer> ();
            labelRenderer.material = label.font.material;
            labelRenderer.enabled = false;
            Color = new Tuple3 (1, 1, 1);
            Thickness = 0.1f;
        }

        // An arrow head is a line of its own, a child of the path, that tapers to a point
        LineRenderer CreateHead (string name)
        {
            var obj = new GameObject (GameObject.name + "." + name);
            obj.transform.SetParent (GameObject.transform, false);
            var head = obj.AddComponent<LineRenderer> ();
            head.useWorldSpace = true;
            head.positionCount = 2;
            head.enabled = false;
            return head;
        }

        /// <summary>
        /// The points of the path, in world space.
        /// </summary>
        protected abstract IList<Vector3d> WorldPoints ();

        /// <summary>
        /// Whether the path joins its last point back to its first.
        /// </summary>
        protected abstract bool Closed { get; }

        /// <summary>
        /// Update the path.
        /// </summary>
        public override void Update ()
        {
            if (!CanBeDrawn) {
                Hide ();
                return;
            }
            var worldPoints = WorldPoints ();
            points.Clear ();
            foreach (var point in worldPoints)
                points.Add (View.ToView (point));
            if (Closed && points.Count > 0)
                points.Add (points [0]);
            if (points.Count < 2) {
                Hide ();
                return;
            }
            DrawLabel ();

            // Arrow heads take their length from the end of the path, and share it between
            // them when the path is shorter than both
            var startTip = points [0];
            var endTip = points [points.Count - 1];
            var startLength = StartArrow ? HeadLength (startTip) : 0f;
            var endLength = EndArrow ? HeadLength (endTip) : 0f;
            var pathLength = Length (points);
            if (startLength + endLength > pathLength) {
                var scale = pathLength / (startLength + endLength);
                startLength *= scale;
                endLength *= scale;
            }
            if (StartArrow)
                DrawHead (startHead, TrimStart (points, startLength), startTip, startLength, colors [0]);
            if (EndArrow) {
                points.Reverse ();
                var endBase = TrimStart (points, endLength);
                points.Reverse ();
                DrawHead (endHead, endBase, endTip, endLength, colors [colors.Count - 1]);
            }
            startHead.enabled = Visible && StartArrow;
            endHead.enabled = Visible && EndArrow;

            if (Dashed)
                Dash (points);
            renderer.textureMode = Dashed ? LineTextureMode.RepeatPerSegment : LineTextureMode.Stretch;
            renderer.sharedMaterial.mainTexture = Dashed ? DashTexture : null;
            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            renderer.positionCount = points.Count;
            renderer.SetPositions (points.ToArray ());
            renderer.widthCurve = View.Scaled ? WidthCurve (points) : constantWidth;
            renderer.widthMultiplier = thickness;
        }

        /// <summary>
        /// Destroy the path.
        /// </summary>
        public override void Destroy ()
        {
            UnityEngine.Object.Destroy (headMaterial);
            base.Destroy ();
        }

        void Hide ()
        {
            renderer.enabled = false;
            startHead.enabled = false;
            endHead.enabled = false;
            labelRenderer.enabled = false;
        }

        // The label sits halfway along the path, facing the camera
        void DrawLabel ()
        {
            labelRenderer.enabled = Visible && label.text.Length > 0 && View.Camera != null;
            if (!labelRenderer.enabled)
                return;
            var half = 0.5f * Length (points);
            var position = points [points.Count - 1];
            for (int i = 1; i < points.Count; i++) {
                var segment = Vector3.Distance (points [i - 1], points [i]);
                if (segment >= half) {
                    position = Vector3.MoveTowards (points [i - 1], points [i], half);
                    break;
                }
                half -= segment;
            }
            var transform = labelRenderer.transform;
            labelRenderer.gameObject.layer = View.Layer;
            transform.position = position;
            transform.rotation = View.Camera.transform.rotation;
            transform.localScale = Vector3.one * View.SizeAt (position);
            label.color = renderer.colorGradient.Evaluate (0.5f);
        }

        float HeadLength (Vector3 tip)
        {
            return 2f * arrowWidth * arrowSize * thickness * View.SizeAt (tip);
        }

        void DrawHead (LineRenderer head, Vector3 headBase, Vector3 tip, float length, Tuple3 color)
        {
            head.gameObject.layer = View.Layer;
            // A head is drawn solid, so it has a material of its own without the dash texture
            if (headMaterial == null || headMaterial.shader != renderer.sharedMaterial.shader) {
                UnityEngine.Object.Destroy (headMaterial);
                headMaterial = new Material (renderer.sharedMaterial.shader);
            }
            head.sharedMaterial = headMaterial;
            head.startColor = color.ToColor ();
            head.endColor = color.ToColor ();
            head.SetPosition (0, headBase);
            head.SetPosition (1, tip);
            head.widthMultiplier = 1f;
            head.startWidth = 0.5f * length;
            head.endWidth = 0f;
        }

        // A two texel texture, the first lit and the second clear, stretched over one dash and gap
        static Texture2D DashTexture {
            get {
                if (dashTexture == null) {
                    dashTexture = new Texture2D (2, 1, TextureFormat.RGBA32, false);
                    dashTexture.filterMode = FilterMode.Point;
                    dashTexture.wrapMode = TextureWrapMode.Repeat;
                    dashTexture.SetPixels (new [] { UnityEngine.Color.white, UnityEngine.Color.clear });
                    dashTexture.Apply ();
                }
                return dashTexture;
            }
        }

        // The length of one dash and gap at the given point
        float DashPeriod (Vector3 point)
        {
            return 2f * dashLength * thickness * View.SizeAt (point);
        }

        // Split the path into pieces of one dash and gap each, each drawn with one repeat of
        // the dash texture. The pieces restart at each corner, so that the corner is drawn.
        void Dash (List<Vector3> path)
        {
            var dashed = new List<Vector3> { path [0] };
            var runStart = 0;
            for (int i = 1; i < path.Count; i++) {
                if (i == path.Count - 1 ||
                    Vector3.Angle (path [i] - path [i - 1], path [i + 1] - path [i]) > cornerAngle) {
                    DashRun (path, runStart, i, dashed);
                    runStart = i;
                }
            }
            path.Clear ();
            path.AddRange (dashed);
        }

        // Add the pieces between two corners, sized by the dash length along the way. A curve
        // between the corners is drawn through the ends of the pieces.
        void DashRun (List<Vector3> path, int start, int end, List<Vector3> dashed)
        {
            // The number of pieces that fit along each segment
            var counts = new float [end - start];
            var total = 0f;
            for (int i = 0; i < counts.Length; i++) {
                var a = path [start + i];
                var b = path [start + i + 1];
                var period = DashPeriod (0.5f * (a + b));
                counts [i] = period > 0f ? Vector3.Distance (a, b) / period : 0f;
                total += counts [i];
            }
            var pieces = Mathf.Clamp (Mathf.RoundToInt (total), 1, maxDashes);
            var step = total / pieces;
            var next = 1;
            var reached = 0f;
            for (int i = 0; i < counts.Length; i++) {
                while (next < pieces && counts [i] > 0f && reached + counts [i] >= next * step) {
                    var fraction = (next * step - reached) / counts [i];
                    dashed.Add (Vector3.Lerp (path [start + i], path [start + i + 1], fraction));
                    next++;
                }
                reached += counts [i];
            }
            dashed.Add (path [end]);
        }

        static float Length (IList<Vector3> points)
        {
            var length = 0f;
            for (int i = 1; i < points.Count; i++)
                length += Vector3.Distance (points [i - 1], points [i]);
            return length;
        }

        // Remove the given length from the start of the path, and return the point it now starts at
        static Vector3 TrimStart (List<Vector3> points, float length)
        {
            while (points.Count > 2) {
                var segment = Vector3.Distance (points [0], points [1]);
                if (segment > length)
                    break;
                length -= segment;
                points.RemoveAt (0);
            }
            points [0] = Vector3.MoveTowards (points [0], points [1], length);
            return points [0];
        }

        // The width at each point, keyed by the fraction of the distance along the path
        static AnimationCurve WidthCurve (IList<Vector3> points)
        {
            var widths = new Keyframe [points.Count];
            var length = 0f;
            for (int i = 0; i < points.Count; i++) {
                if (i > 0)
                    length += Vector3.Distance (points [i - 1], points [i]);
                widths [i] = new Keyframe (length, View.SizeAt (points [i]));
            }
            if (length > 0f) {
                for (int i = 0; i < widths.Length; i++)
                    widths [i].time /= length;
            }
            return new AnimationCurve (widths);
        }

        // The line renderer, checked to still exist.
        LineRenderer LineRenderer {
            get {
                CheckExists ();
                return renderer;
            }
        }

        /// <summary>
        /// The color of the path.
        /// </summary>
        /// <remarks>
        /// For a path drawn with more than one color, this is the color at its start.
        /// </remarks>
        [KRPCProperty]
        public Tuple3 Color {
            get { return colors [0]; }
            set { Colors = new [] { value }; }
        }

        /// <summary>
        /// The colors of the path, blended evenly from its start to its end.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the list is empty or holds more than 8 colors.
        /// </remarks>
        [KRPCProperty]
        public IList<Tuple3> Colors {
            get { return colors; }
            set {
                if (value.Count == 0 || value.Count > maxColors)
                    throw new ArgumentException ("A path has between 1 and " + maxColors + " colors");
                var lineRenderer = LineRenderer;
                colors = value.ToList ();
                var keys = new GradientColorKey [colors.Count];
                for (int i = 0; i < keys.Length; i++)
                    keys [i] = new GradientColorKey (colors [i].ToColor (), keys.Length == 1 ? 0f : (float)i / (keys.Length - 1));
                var gradient = new Gradient ();
                gradient.SetKeys (keys, new [] { new GradientAlphaKey (1f, 0f) });
                lineRenderer.colorGradient = gradient;
            }
        }

        /// <summary>
        /// Whether an arrow head is drawn at the start.
        /// </summary>
        [KRPCProperty]
        public bool StartArrow { get; set; }

        /// <summary>
        /// Whether an arrow head is drawn at the end.
        /// </summary>
        [KRPCProperty]
        public bool EndArrow { get; set; }

        /// <summary>
        /// The size of the arrow heads, relative to the thickness.
        /// </summary>
        [KRPCProperty]
        public float ArrowSize {
            get { return arrowSize; }
            set { arrowSize = value; }
        }

        /// <summary>
        /// Text drawn halfway along the path, facing the camera. Empty for no label.
        /// </summary>
        [KRPCProperty]
        public string Label {
            get { return labelText; }
            set {
                CheckExists ();
                labelText = value;
                label.text = value;
            }
        }

        /// <summary>
        /// Whether the path is drawn as a dashed line.
        /// </summary>
        [KRPCProperty]
        public bool Dashed { get; set; }

        /// <summary>
        /// The thickness of the path.
        /// </summary>
        [KRPCProperty]
        public float Thickness {
            get { return thickness; }
            set {
                var lineRenderer = LineRenderer;
                thickness = value;
                lineRenderer.widthMultiplier = thickness;
            }
        }
    }
}
