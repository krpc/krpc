using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
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
        static class QuaternionIdentity
        {
            public static object Create ()
            {
                return new Tuple4 (0, 0, 0, 1);
            }
        }

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
        /// Draw the axes of a reference frame in the scene, as arrows from its origin.
        /// </summary>
        /// <param name="referenceFrame">The reference frame.</param>
        /// <param name="length">The length of each axis.</param>
        /// <param name="visible">Whether the axes are visible.</param>
        /// <returns>The lines for the x, y and z axes, colored red, green and blue.</returns>
        [KRPCProcedure]
        public static IList<Line> AddAxes (ReferenceFrame referenceFrame, float length = 10f, bool visible = true)
        {
            var axes = new [] {
                new Tuple3 (1, 0, 0),
                new Tuple3 (0, 1, 0),
                new Tuple3 (0, 0, 1)
            };
            var lines = new List<Line> ();
            foreach (var axis in axes) {
                var line = new Line (Vector3d.zero, axis.ToVector () * length, referenceFrame, visible);
                line.Color = new ColorRGB ((float)axis.Item1, (float)axis.Item2, (float)axis.Item3);
                line.EndArrow = true;
                lines.Add (line);
            }
            return lines;
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
        /// Draw the path of an orbit in the scene.
        /// </summary>
        /// <param name="orbit">The orbit.</param>
        /// <param name="visible">Whether the path is visible.</param>
        /// <remarks>
        /// The path follows the orbit as it changes, and ends where the orbit leaves the
        /// sphere of influence of its body.
        /// </remarks>
        [KRPCProcedure (GameScene = GameScene.Flight | GameScene.TrackingStation)]
        public static OrbitPath AddOrbit (SpaceCenter.Services.Orbit orbit, bool visible = true)
        {
            return new OrbitPath (orbit, visible);
        }

        /// <summary>
        /// Draw the edges of a box in the scene.
        /// </summary>
        /// <param name="center">Position of the center of the box.</param>
        /// <param name="size">Lengths of the sides of the box along its x, y and z axes, in meters.</param>
        /// <param name="referenceFrame">Reference frame that the position and rotation are in.</param>
        /// <param name="rotation">Rotation of the box, as a quaternion.</param>
        /// <param name="visible">Whether the box is visible.</param>
        [KRPCProcedure]
        public static Box AddBox (
            Tuple3 center, Tuple3 size, ReferenceFrame referenceFrame,
            [KRPCDefaultValue (typeof(QuaternionIdentity))] Tuple4 rotation, bool visible = true)
        {
            return new Box (center.ToVector (), size.ToVector (), rotation.ToQuaternion (), referenceFrame, visible);
        }

        /// <summary>
        /// Draw a point in the scene, as a dot that keeps its size on screen.
        /// </summary>
        /// <param name="position">Position of the point.</param>
        /// <param name="referenceFrame">Reference frame that the position is in.</param>
        /// <param name="visible">Whether the point is visible.</param>
        [KRPCProcedure]
        public static Point AddPoint (Tuple3 position, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new Point (position.ToVector (), referenceFrame, visible);
        }

        /// <summary>
        /// Draw a solid sphere in the scene.
        /// </summary>
        /// <param name="center">Position of the center of the sphere.</param>
        /// <param name="radius">Radius of the sphere, in meters.</param>
        /// <param name="referenceFrame">Reference frame that the position is in.</param>
        /// <param name="visible">Whether the sphere is visible.</param>
        [KRPCProcedure]
        public static Sphere AddSphere (Tuple3 center, double radius, ReferenceFrame referenceFrame, bool visible = true)
        {
            return new Sphere (center.ToVector (), radius, referenceFrame, visible);
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
        /// Draw a marker in the map view, with one of the game's own map icons.
        /// </summary>
        /// <param name="position">Position of the marker.</param>
        /// <param name="referenceFrame">Reference frame that the position is in.</param>
        /// <param name="icon">The icon that the marker is drawn with.</param>
        /// <param name="caption">The caption shown when the mouse is over the marker.</param>
        /// <param name="visible">Whether the marker is visible.</param>
        [KRPCProcedure (GameScene = GameScene.Flight | GameScene.TrackingStation)]
        public static MapMarker AddMapMarker (Tuple3 position, ReferenceFrame referenceFrame, MapMarkerIcon icon = MapMarkerIcon.Generic, string caption = "", bool visible = true)
        {
            return new MapMarker (position.ToVector (), referenceFrame, icon, caption, visible);
        }

        /// <summary>
        /// Load an image that markers can be drawn with, from the contents of a PNG or JPEG
        /// file. The transparency of a PNG image is kept.
        /// </summary>
        /// <param name="data">The contents of the image file.</param>
        /// <remarks>
        /// Throws an exception if the data is not a PNG or JPEG image.
        /// </remarks>
        [KRPCProcedure]
        public static Image AddImage (byte[] data)
        {
            return new Image (data);
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
