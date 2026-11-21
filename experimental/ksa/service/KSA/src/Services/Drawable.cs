using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The members shared by the drawing objects.
    /// </summary>
    public abstract class Drawable : IGameObjectState
    {
        ReferenceFrame referenceFrame;
        bool removed;

        /// <summary>
        /// Create a drawing object, in white.
        /// </summary>
        protected Drawable (ReferenceFrame referenceFrame, bool visible)
        {
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new ColorRGB (1, 1, 1);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return removed ? GameObjectState.Destroyed : GameObjectState.Live; }
        }

        /// <summary>
        /// Mark the object as removed. Called when it leaves the list of objects.
        /// </summary>
        internal void Removed ()
        {
            removed = true;
        }

        /// <summary>
        /// The reference frame for the positions and directions of the object.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame {
            get { return referenceFrame; }
            set { referenceFrame = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// Whether the object is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible { get; set; }

        /// <summary>
        /// The color of the object, as an RGB triple with components from 0 to 1.
        /// </summary>
        [KRPCProperty]
        public ColorRGB Color { get; set; }

        /// <summary>
        /// Remove the object.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            Drawables.Remove (this);
        }

        /// <summary>
        /// Draw the object's gizmos for the current frame.
        /// </summary>
        internal virtual void DrawGizmos (GizmosRenderer gizmos, global::KSA.Camera camera)
        {
        }

        /// <summary>
        /// Draw the object on the overlay for the current frame. The origin is the screen
        /// position of the viewport, in pixels.
        /// </summary>
        internal virtual void DrawOverlay (ImDrawListPtr drawList, global::KSA.Camera camera, float2 origin)
        {
        }

        /// <summary>
        /// The color as a gizmo color.
        /// </summary>
        protected float4 GizmoColor {
            get { return new float4 (Color.R, Color.G, Color.B, 1f); }
        }

        /// <summary>
        /// A position in the reference frame, relative to the camera.
        /// </summary>
        protected double3 ToEgo (global::KSA.Camera camera, double3 position)
        {
            return camera.EclToEgo (ReferenceFrame.PositionToEcl (position));
        }

        /// <summary>
        /// A direction in the reference frame, in the camera's axes, which are ecliptic.
        /// </summary>
        protected double3 DirectionToEgo (double3 direction)
        {
            return ReferenceFrame.DirectionToEcl (direction);
        }

        /// <summary>
        /// Draw an arc of line segments in the reference frame. The axes are the vectors from
        /// the center to the points at angle 0 and at a quarter turn, in meters.
        /// </summary>
        protected void DrawArc (
            GizmosRenderer gizmos, global::KSA.Camera camera,
            double3 center, double3 axisA, double3 axisB, double angle)
        {
            // The game's own arcs have 12 segments per turn, too coarse at a distance
            var segments = Math.Max (2, (int)Math.Ceiling (64 * Math.Abs (angle) / (2 * Math.PI)));
            var centerEgo = ToEgo (camera, center);
            var a = DirectionToEgo (axisA);
            var b = DirectionToEgo (axisB);
            var color = GizmoColor;
            var start = centerEgo + a;
            for (int i = 1; i <= segments; i++) {
                var theta = angle * i / segments;
                var end = centerEgo + a * Math.Cos (theta) + b * Math.Sin (theta);
                gizmos.DrawLine (start, end, color);
                start = end;
            }
        }
    }
}
