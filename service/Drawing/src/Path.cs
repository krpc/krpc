using System.Collections.Generic;
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

        readonly LineRenderer renderer;
        readonly LineRenderer startHead;
        readonly LineRenderer endHead;
        readonly List<Vector3> points = new List<Vector3> ();
        Tuple3 color;
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
                DrawHead (startHead, TrimStart (points, startLength), startTip, startLength);
            if (EndArrow) {
                points.Reverse ();
                var endBase = TrimStart (points, endLength);
                points.Reverse ();
                DrawHead (endHead, endBase, endTip, endLength);
            }
            startHead.enabled = Visible && StartArrow;
            endHead.enabled = Visible && EndArrow;

            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            renderer.positionCount = points.Count;
            renderer.SetPositions (points.ToArray ());
            renderer.widthCurve = View.Scaled ? WidthCurve (points) : constantWidth;
            renderer.widthMultiplier = thickness;
        }

        void Hide ()
        {
            renderer.enabled = false;
            startHead.enabled = false;
            endHead.enabled = false;
        }

        float HeadLength (Vector3 tip)
        {
            return 2f * arrowWidth * arrowSize * thickness * View.SizeAt (tip);
        }

        void DrawHead (LineRenderer head, Vector3 headBase, Vector3 tip, float length)
        {
            head.gameObject.layer = View.Layer;
            head.sharedMaterial = renderer.sharedMaterial;
            head.startColor = renderer.startColor;
            head.endColor = renderer.endColor;
            head.SetPosition (0, headBase);
            head.SetPosition (1, tip);
            head.widthMultiplier = 1f;
            head.startWidth = 0.5f * length;
            head.endWidth = 0f;
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
        [KRPCProperty]
        public Tuple3 Color {
            get { return color; }
            set {
                var lineRenderer = LineRenderer;
                color = value;
                var rgbColor = color.ToColor ();
                lineRenderer.startColor = rgbColor;
                lineRenderer.endColor = rgbColor;
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
