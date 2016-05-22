using KRPC.Service;
using KRPC.Service.Attributes;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;
using Tuple4 = KRPC.Utils.Tuple<double, double, double, double>;

namespace KRPC.UI
{
    /// <summary>
    /// Provides functionality for drawing and interacting with in-game user interface elements.
    /// </summary>
    /// <remarks>
    /// For drawing 3D objects in the flight scene, see the Drawing service.
    /// </remarks>
    [KRPCService (GameScene = GameScene.All)]
    public static class UI
    {
        /// <summary>
        /// The rect transform for the canvas.
        /// </summary>
        [KRPCProperty]
        public static RectTransform RectTransform {
            get{ return new RectTransform (Addon.Canvas.GetComponent<UnityEngine.RectTransform> ()); }
        }

        /// <summary>
        /// Create a new container for user interface elements.
        /// </summary>
        /// <param name="visible">Whether the panel is visible.</param>
        [KRPCProcedure]
        public static Panel AddPanel (bool visible = true)
        {
            return new Panel (Addon.Canvas, visible);
        }

        /// <summary>
        /// Remove all user interface elements.
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
