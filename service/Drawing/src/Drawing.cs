using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// Provides functionality for drawing objects in the flight scene.
    /// </summary>
    /// <remarks>
    /// For drawing and interacting with the user interface, see the UI service.
    /// </remarks>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class Drawing
    {
        /// <summary>
        /// Draw a line in the scene.
        /// </summary>
        /// <param name="start">Position of the start of the line.</param>
        /// <param name="end">Position of the end of the line.</param>
        /// <param name="referenceFrame">Reference frame that the positions are in.</param>
        [KRPCProcedure]
        public static Line AddLine (Tuple3 start, Tuple3 end, ReferenceFrame referenceFrame)
        {
            return new Line (start.ToVector (), end.ToVector (), referenceFrame);
        }

        /// <summary>
        /// Draw a direction vector in the scene, from the center of mass of the active vessel.
        /// </summary>
        /// <param name="direction">Direction to draw the line in.</param>
        /// <param name="referenceFrame">Reference frame that the direction is in.</param>
        /// <param name="length">The length of the line.</param>
        [KRPCProcedure]
        public static Line AddDirection (Tuple3 direction, ReferenceFrame referenceFrame, float length = 10f)
        {
            return new Line (Vector3d.zero, direction.ToVector () * length, referenceFrame);
        }

        /// <summary>
        /// Draw a polygon in the scene, defined by a list of vertices.
        /// </summary>
        /// <param name="vertices">Vertices of the polygon.</param>
        /// <param name="referenceFrame">Reference frame that the vertices are in.</param>
        [KRPCProcedure]
        public static Polygon AddPolygon (IList<Tuple3> vertices, ReferenceFrame referenceFrame)
        {
            return new Polygon (vertices.Select (x => x.ToVector ()).ToList (), referenceFrame);
        }

        /// <summary>
        /// Draw text in the scene.
        /// </summary>
        /// <param name="text">The string to draw.</param>
        /// <param name="referenceFrame">Reference frame that the text position is in.</param>
        /// <param name="position">Position of the text.</param>
        /// <param name="rotation">Rotation of the text, as a quaternion.</param>
        [KRPCProcedure]
        public static Text AddText (string text, ReferenceFrame referenceFrame, Tuple3 position, Tuple4 rotation)
        {
            return new Text (text, referenceFrame, position.ToVector (), rotation.ToQuaternion ());
        }

        /// <summary>
        /// Remove all objects being drawn.
        /// </summary>
        /// <param name="clientOnly">If true, only remove objects created by the calling client.</param>
        [KRPCProcedure]
        public static void Clear (bool clientOnly = false)
        {
            if (clientOnly)
                Addon.Clear (KRPCServer.Context.RPCClient);
            else
                Addon.Clear ();
        }
    }
}
