using System.Collections.Generic;
using KRPC.Utils;
using KRPC.SpaceCenter.Services;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using System.Linq;

namespace KRPC.SpaceCenter
{
    using Direction = Tuple<KRPC.Server.IClient, GameObject, LineRenderer, Vector3, float, ReferenceFrame>;
    using Line = Tuple<KRPC.Server.IClient, GameObject, LineRenderer, Vector3, Vector3, ReferenceFrame>;

    /// <summary>
    /// Addon for visual debugging functionality
    /// </summary>
    [KSPAddon (KSPAddon.Startup.Flight, false)]
    public class DrawAddon : MonoBehaviour
    {
        static IList<Direction> directions = new List<Direction> ();
        static IList<Line> lines = new List<Line> ();

        internal static void AddDirection (Vector3 direction, ReferenceFrame referenceFrame, Tuple3 color, float length)
        {
            var obj = new GameObject ("direction");
            var line = obj.AddComponent<LineRenderer> ();
            line.useWorldSpace = true;
            line.material = new Material (Shader.Find ("Particles/Additive"));
            line.SetWidth (0.25f, 0.25f);
            line.SetVertexCount (2);
            line.SetPosition (0, Vector3d.zero);
            line.SetPosition (1, Vector3d.zero);
            var rgbColor = new Color ((float)color.Item1, (float)color.Item2, (float)color.Item3);
            line.SetColors (rgbColor, rgbColor);
            directions.Add (new Direction (KRPC.KRPCServer.Context.RPCClient, obj, line, direction, length, referenceFrame));
        }

        internal static void AddLine (Vector3 start, Vector3 end, ReferenceFrame referenceFrame, Tuple3 color)
        {
            var obj = new GameObject ("vector");
            var line = obj.AddComponent<LineRenderer> ();
            line.useWorldSpace = true;
            line.material = new Material (Shader.Find ("Particles/Additive"));
            line.SetWidth (0.25f, 0.25f);
            line.SetVertexCount (2);
            line.SetPosition (0, Vector3d.zero);
            line.SetPosition (1, Vector3d.zero);
            var rgbColor = new Color ((float)color.Item1, (float)color.Item2, (float)color.Item3);
            line.SetColors (rgbColor, rgbColor);
            lines.Add (new Line (KRPC.KRPCServer.Context.RPCClient, obj, line, start, end, referenceFrame));
        }

        internal static void ClearDrawing ()
        {
            foreach (var direction in directions)
                Object.Destroy (direction.Item2);
            directions.Clear ();
            foreach (var line in lines)
                Object.Destroy (line.Item2);
            lines.Clear ();
        }

        /// <summary>
        /// Wake the addon
        /// </summary>
        public void Awake ()
        {
        }

        /// <summary>
        /// Update the addon
        /// </summary>
        public void Update ()
        {
            // Remove directions and lines for disconnected clients
            if (directions.Any ()) {
                foreach (var direction in directions) {
                    if (!direction.Item1.Connected)
                        Object.Destroy (direction.Item2);
                }
                directions = directions.Where (x => x.Item1.Connected).ToList ();
            }
            if (lines.Any ()) {
                foreach (var line in lines) {
                    if (!line.Item1.Connected)
                        Object.Destroy (line.Item2);
                }
                lines = lines.Where (x => x.Item1.Connected).ToList ();
            }

            // Render directions on the active vessel
            var vessel = FlightGlobals.ActiveVessel;
            if (vessel != null) {
                var origin = vessel.CoM;
                foreach (var direction in directions) {
                    direction.Item3.SetPosition (0, origin);
                    direction.Item3.SetPosition (1, origin + (direction.Item6.DirectionToWorldSpace (direction.Item4) * direction.Item5));
                }
            }

            // Render lines
            foreach (var line in lines) {
                line.Item3.SetPosition (0, line.Item6.PositionToWorldSpace (line.Item4));
                line.Item3.SetPosition (1, line.Item6.PositionToWorldSpace (line.Item5));
            }
        }

        /// <summary>
        /// Destroy the addon
        /// </summary>
        public void OnDestroy ()
        {
            ClearDrawing ();
        }
    }
}
