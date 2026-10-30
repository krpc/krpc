using System;
using System.Collections.Generic;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Draws lines, shapes and text in the scene, each placed in a reference frame.
    /// </summary>
    /// <remarks>
    /// The objects are drawn every frame until removed, and are removed when the client that
    /// added them disconnects. Lines are drawn at the game's fixed gizmo width.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public sealed class Drawing
    {
        // The class holds static methods only
        Drawing ()
        {
        }

        /// <summary>
        /// Draw a line between two positions.
        /// </summary>
        /// <param name="start">The position of the start of the line, in meters.</param>
        /// <param name="end">The position of the end of the line, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the positions.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCMethod]
        public static Line AddLine (
            Tuple<double, double, double> start, Tuple<double, double, double> end,
            ReferenceFrame referenceFrame, bool visible = true)
        {
            return Drawables.Add (new Line (start, end, referenceFrame, visible));
        }

        /// <summary>
        /// Draw a line from the origin of a reference frame in a direction.
        /// </summary>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="referenceFrame">The reference frame of the direction.</param>
        /// <param name="length">The length of the line, in meters.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCMethod]
        public static Line AddDirection (
            Tuple<double, double, double> direction, ReferenceFrame referenceFrame,
            float length = 10, bool visible = true)
        {
            var end = Vectors.ToUnitVector (direction, nameof (direction)) * length;
            return Drawables.Add (new Line (Tuple.Create (0.0, 0.0, 0.0), Vectors.ToTuple (end), referenceFrame, visible));
        }

        /// <summary>
        /// Draw a line through a list of positions.
        /// </summary>
        /// <param name="vertices">The positions the line passes through, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the positions.</param>
        /// <param name="closed">Whether the line joins the last position back to the first.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCMethod]
        public static Polyline AddPolyline (
            IList<Tuple<double, double, double>> vertices, ReferenceFrame referenceFrame,
            bool closed = false, bool visible = true)
        {
            return Drawables.Add (new Polyline (vertices, closed, referenceFrame, visible));
        }

        /// <summary>
        /// Draw a circle.
        /// </summary>
        /// <param name="center">The position of the center of the circle, in meters.</param>
        /// <param name="normal">The direction normal to the plane of the circle.</param>
        /// <param name="radius">The radius of the circle, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position and direction.</param>
        /// <param name="visible">Whether the circle is visible.</param>
        [KRPCMethod]
        public static Circle AddCircle (
            Tuple<double, double, double> center, Tuple<double, double, double> normal, float radius,
            ReferenceFrame referenceFrame, bool visible = true)
        {
            return Drawables.Add (new Circle (center, normal, radius, referenceFrame, visible));
        }

        /// <summary>
        /// Draw an arc of a circle.
        /// </summary>
        /// <param name="center">The position of the center of the arc's circle, in meters.</param>
        /// <param name="normal">The direction normal to the plane of the arc.</param>
        /// <param name="startDirection">The direction from the center to the start of the arc.</param>
        /// <param name="angle">The angle the arc turns through, counterclockwise about the normal, in radians.</param>
        /// <param name="radius">The radius of the arc, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position and directions.</param>
        /// <param name="visible">Whether the arc is visible.</param>
        [KRPCMethod]
        public static Arc AddArc (
            Tuple<double, double, double> center, Tuple<double, double, double> normal,
            Tuple<double, double, double> startDirection, float angle, float radius,
            ReferenceFrame referenceFrame, bool visible = true)
        {
            return Drawables.Add (new Arc (center, normal, startDirection, angle, radius, referenceFrame, visible));
        }

        /// <summary>
        /// Draw the edges of a box.
        /// </summary>
        /// <param name="center">The position of the center of the box, in meters.</param>
        /// <param name="size">The lengths of the box's edges along its x, y and z axes, in meters.</param>
        /// <param name="rotation">The rotation of the box's axes, as a quaternion of the form <c>(x, y, z, w)</c>.</param>
        /// <param name="referenceFrame">The reference frame of the position and rotation.</param>
        /// <param name="visible">Whether the box is visible.</param>
        [KRPCMethod]
        public static Box AddBox (
            Tuple<double, double, double> center, Tuple<double, double, double> size,
            Tuple<double, double, double, double> rotation, ReferenceFrame referenceFrame, bool visible = true)
        {
            return Drawables.Add (new Box (center, size, rotation, referenceFrame, visible));
        }

        /// <summary>
        /// Draw a solid sphere.
        /// </summary>
        /// <param name="center">The position of the center of the sphere, in meters.</param>
        /// <param name="radius">The radius of the sphere, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        /// <param name="visible">Whether the sphere is visible.</param>
        [KRPCMethod]
        public static Sphere AddSphere (
            Tuple<double, double, double> center, float radius, ReferenceFrame referenceFrame, bool visible = true)
        {
            return Drawables.Add (new Sphere (center, radius, referenceFrame, visible));
        }

        /// <summary>
        /// Draw text over the scene, centered on a position.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="position">The position of the center of the text, in meters.</param>
        /// <param name="referenceFrame">The reference frame of the position.</param>
        /// <param name="size">The height of the font, in pixels.</param>
        /// <param name="visible">Whether the text is visible.</param>
        [KRPCMethod]
        public static Text AddText (
            string text, Tuple<double, double, double> position, ReferenceFrame referenceFrame,
            float size = 16, bool visible = true)
        {
            return Drawables.Add (new Text (text, position, size, referenceFrame, visible));
        }

        /// <summary>
        /// Remove the drawing objects.
        /// </summary>
        /// <param name="clientOnly">Whether to remove only the objects added by the calling client.</param>
        [KRPCMethod]
        public static void Clear (bool clientOnly = false)
        {
            Drawables.Clear (clientOnly);
        }
    }
}
