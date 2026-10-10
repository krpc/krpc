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

        readonly LineRenderer renderer;
        readonly List<Vector3> points = new List<Vector3> ();
        Tuple3 color;
        float thickness;

        /// <summary>
        /// Create a path.
        /// </summary>
        protected Path ()
            : base (typeof(LineRenderer))
        {
            renderer = GameObject.GetComponent<LineRenderer> ();
            renderer.useWorldSpace = true;
            renderer.positionCount = 0;
            Color = new Tuple3 (1, 1, 1);
            Thickness = 0.1f;
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
                renderer.enabled = false;
                return;
            }
            var worldPoints = WorldPoints ();
            if (worldPoints.Count == 0) {
                renderer.enabled = false;
                return;
            }
            renderer.enabled = Visible;
            GameObject.layer = View.Layer;
            points.Clear ();
            foreach (var point in worldPoints)
                points.Add (View.ToView (point));
            if (Closed)
                points.Add (points [0]);
            renderer.positionCount = points.Count;
            renderer.SetPositions (points.ToArray ());
            renderer.widthCurve = View.Scaled ? WidthCurve (points) : constantWidth;
            renderer.widthMultiplier = thickness;
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
