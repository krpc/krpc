using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// The edges of a box. Created using <see cref="Drawing.AddBox" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class Box : Drawable<Box>
    {
        // The corners of a box with sides of length 2, centered on the origin
        static readonly Vector3d[] corners = {
            new Vector3d (-1, -1, -1), new Vector3d (1, -1, -1),
            new Vector3d (1, -1, 1), new Vector3d (-1, -1, 1),
            new Vector3d (-1, 1, -1), new Vector3d (1, 1, -1),
            new Vector3d (1, 1, 1), new Vector3d (-1, 1, 1)
        };

        // The corners each line runs through: the bottom and top faces as loops, then the
        // four edges between them. No edge is drawn twice, as the additive material would
        // draw it brighter.
        static readonly int[][] strokes = {
            new [] { 0, 1, 2, 3 },
            new [] { 4, 5, 6, 7 },
            new [] { 0, 4 },
            new [] { 1, 5 },
            new [] { 2, 6 },
            new [] { 3, 7 }
        };

        readonly LineRenderer[] lines = new LineRenderer [strokes.Length];
        readonly List<Vector3> points = new List<Vector3> ();
        Vector3d center;
        Vector3d size;
        QuaternionD rotation;
        ColorRGB color;
        float thickness;

        internal Box (Vector3d boxCenter, Vector3d boxSize, QuaternionD boxRotation, ReferenceFrame referenceFrame, bool visible)
            : base (typeof(LineRenderer))
        {
            for (int i = 0; i < lines.Length; i++) {
                LineRenderer line;
                if (i == 0) {
                    line = GameObject.GetComponent<LineRenderer> ();
                } else {
                    var obj = new GameObject (GameObject.name + ".Edge" + i);
                    obj.transform.SetParent (GameObject.transform, false);
                    line = obj.AddComponent<LineRenderer> ();
                }
                line.useWorldSpace = true;
                line.loop = strokes [i].Length > 2;
                line.positionCount = strokes [i].Length;
                line.enabled = false;
                lines [i] = line;
            }
            center = boxCenter;
            size = boxSize;
            rotation = boxRotation;
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new ColorRGB (1, 1, 1);
            Thickness = 0.1f;
        }

        /// <summary>
        /// Whether the box can be drawn.
        /// </summary>
        protected override bool CanBeDrawn {
            get { return CanBeDrawnIn (ReferenceFrame); }
        }

        /// <summary>
        /// Update the box.
        /// </summary>
        public override void Update ()
        {
            if (!CanBeDrawn) {
                foreach (var line in lines)
                    line.enabled = false;
                return;
            }
            var viewCorners = new Vector3 [corners.Length];
            for (int i = 0; i < corners.Length; i++) {
                var corner = corners [i];
                var offset = rotation * new Vector3d (0.5 * size.x * corner.x, 0.5 * size.y * corner.y, 0.5 * size.z * corner.z);
                viewCorners [i] = View.ToView (ReferenceFrame.PositionToWorldSpace (center + offset));
            }
            var material = lines [0].sharedMaterial;
            for (int i = 0; i < lines.Length; i++) {
                var line = lines [i];
                points.Clear ();
                foreach (var corner in strokes [i])
                    points.Add (viewCorners [corner]);
                line.enabled = Visible;
                line.gameObject.layer = View.Layer;
                line.sharedMaterial = material;
                line.SetPositions (points.ToArray ());
                line.widthCurve = View.WidthCurve (points);
                line.widthMultiplier = thickness;
                line.startColor = color.ToColor ();
                line.endColor = color.ToColor ();
            }
        }

        /// <summary>
        /// Reference frame for the position and rotation of the box.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Position of the center of the box.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Center {
            get { return center.ToTuple (); }
            set { center = value.ToVector (); }
        }

        /// <summary>
        /// Lengths of the sides of the box along its x, y and z axes, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Size {
            get { return size.ToTuple (); }
            set { size = value.ToVector (); }
        }

        /// <summary>
        /// Rotation of the box, as a quaternion.
        /// </summary>
        [KRPCProperty]
        public Tuple4 Rotation {
            get { return rotation.ToTuple (); }
            set { rotation = value.ToQuaternion (); }
        }

        /// <summary>
        /// The color of the edges.
        /// </summary>
        [KRPCProperty]
        public ColorRGB Color {
            get { return color; }
            set {
                CheckExists ();
                color = value;
            }
        }

        /// <summary>
        /// The thickness of the edges.
        /// </summary>
        [KRPCProperty]
        public float Thickness {
            get { return thickness; }
            set {
                CheckExists ();
                thickness = value;
            }
        }
    }
}
