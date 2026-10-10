using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// Provides functionality for drawing objects in the flight scene, the map view, the editor
    /// and the tracking station.
    /// </summary>
    /// <remarks>
    /// For drawing and interacting with the user interface, see the UI service.
    /// Objects drawn by a client are removed when that client disconnects.
    /// </remarks>
    [KRPCService (Id = 3, GameScene = GameScene.Flight | GameScene.Editor | GameScene.TrackingStation)]
    public static class Drawing
    {
        /// <summary>
        /// Draw a line in the scene.
        /// </summary>
        /// <param name="start">Position of the start of the line.</param>
        /// <param name="end">Position of the end of the line.</param>
        /// <param name="referenceFrame">Reference frame that the positions are in.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCProcedure]
        public static Line AddLine (Tuple3 start, Tuple3 end, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new Line (start.ToVector (), end.ToVector (), referenceFrame, visible);
        }

        /// <summary>
        /// Draw a direction vector in the scene, starting from the origin of the given reference frame.
        /// </summary>
        /// <param name="direction">Direction to draw the line in.</param>
        /// <param name="referenceFrame">Reference frame that the direction is in and defines the start position.</param>
        /// <param name="length">The length of the line.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCProcedure]
        public static Line AddDirection(Tuple3 direction, ReferenceFrame referenceFrame, float length = 10f, bool visible = true)
        {
            return new Line(Vector3d.zero, direction.ToVector() * length, referenceFrame, visible);
        }

        /// <summary>
        /// Draw a direction vector in the scene, from the center of mass of the active vessel.
        /// </summary>
        /// <param name="direction">Direction to draw the line in.</param>
        /// <param name="referenceFrame">Reference frame that the direction is in.</param>
        /// <param name="length">The length of the line.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static Line AddDirectionFromCom(Tuple3 direction, ReferenceFrame referenceFrame, float length = 10f, bool visible = true)
        {
            var activeVesselRefFrame = SpaceCenter.Services.SpaceCenter.ActiveVessel.ReferenceFrame;
            referenceFrame = ReferenceFrame.Hybrid(
                activeVesselRefFrame,
                referenceFrame,
                referenceFrame,
                referenceFrame
            );
            return new Line(Vector3d.zero, direction.ToVector() * length, referenceFrame, visible);
        }

        /// <summary>
        /// Draw a polygon in the scene, defined by a list of vertices.
        /// </summary>
        /// <param name="vertices">Vertices of the polygon.</param>
        /// <param name="referenceFrame">Reference frame that the vertices are in.</param>
        /// <param name="visible">Whether the polygon is visible.</param>
        [KRPCProcedure]
        public static Polygon AddPolygon (IList<Tuple3> vertices, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new Polygon (vertices.Select (x => x.ToVector ()).ToList (), referenceFrame, visible);
        }

        /// <summary>
        /// Draw an open line through a list of vertices in the scene.
        /// </summary>
        /// <param name="vertices">Vertices of the line.</param>
        /// <param name="referenceFrame">Reference frame that the vertices are in.</param>
        /// <param name="visible">Whether the line is visible.</param>
        [KRPCProcedure]
        public static Polyline AddPolyline (IList<Tuple3> vertices, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new Polyline (vertices.Select (x => x.ToVector ()).ToList (), referenceFrame, visible);
        }

        /// <summary>
        /// Draw a circle in the scene.
        /// </summary>
        /// <param name="center">Position of the center of the circle.</param>
        /// <param name="normal">Normal of the plane of the circle.</param>
        /// <param name="radius">Radius of the circle, in meters.</param>
        /// <param name="referenceFrame">Reference frame that the position and normal are in.</param>
        /// <param name="visible">Whether the circle is visible.</param>
        /// <remarks>
        /// Throws an exception if the normal is zero.
        /// </remarks>
        [KRPCProcedure]
        public static Circle AddCircle (Tuple3 center, Tuple3 normal, double radius, ReferenceFrame referenceFrame, bool visible = true)
        {
            Circle.CheckNormal (normal.ToVector ());
            return new Circle (center.ToVector (), normal.ToVector (), radius, referenceFrame, visible);
        }

        /// <summary>
        /// Draw an arc of a circle in the scene.
        /// </summary>
        /// <param name="center">Position of the center of the arc.</param>
        /// <param name="normal">Normal of the plane of the arc.</param>
        /// <param name="startDirection">Direction from the center to the start of the arc.</param>
        /// <param name="angle">Angle of the arc, in radians.</param>
        /// <param name="radius">Radius of the arc, in meters.</param>
        /// <param name="referenceFrame">Reference frame that the position and directions are in.</param>
        /// <param name="visible">Whether the arc is visible.</param>
        /// <remarks>
        /// The arc turns counterclockwise about the normal, seen from the side it points to.
        /// Throws an exception if the normal is zero, or the start direction is parallel to it.
        /// </remarks>
        [KRPCProcedure]
        public static Arc AddArc (Tuple3 center, Tuple3 normal, Tuple3 startDirection, double angle, double radius, ReferenceFrame referenceFrame, bool visible = true)
        {
            Arc.CheckPlane (normal.ToVector (), startDirection.ToVector ());
            return new Arc (center.ToVector (), normal.ToVector (), startDirection.ToVector (), angle, radius, referenceFrame, visible);
        }

        /// <summary>
        /// Draw text in the scene.
        /// </summary>
        /// <param name="text">The string to draw.</param>
        /// <param name="referenceFrame">Reference frame that the text position is in.</param>
        /// <param name="position">Position of the text.</param>
        /// <param name="rotation">Rotation of the text, as a quaternion.</param>
        /// <param name="visible">Whether the text is visible.</param>
        [KRPCProcedure]
        public static Text AddText (string text, ReferenceFrame referenceFrame, Tuple3 position, Tuple4 rotation, bool visible = true)
        {
            return new Text (text, referenceFrame, position.ToVector (), rotation.ToQuaternion (), visible);
        }

        /// <summary>
        /// Draw a marker on the navball, pointing in the given direction.
        /// </summary>
        /// <param name="direction">Direction that the marker points in.</param>
        /// <param name="referenceFrame">Reference frame that the direction is in.</param>
        /// <param name="visible">Whether the marker is visible.</param>
        [KRPCProcedure (GameScene = GameScene.Flight)]
        public static NavballMarker AddNavballMarker (Tuple3 direction, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new NavballMarker (direction.ToVector (), referenceFrame, visible);
        }

        /// <summary>
        /// Remove all objects being drawn.
        /// </summary>
        /// <param name="clientOnly">If true, only remove objects created by the calling client.</param>
        [KRPCProcedure]
        public static void Clear (bool clientOnly = false)
        {
            Addon.Clear (clientOnly);
        }
    }
}
