using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;
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
        /// Display a message on the screen.
        /// </summary>
        /// <remarks>
        /// The message appears just like a stock message, for example quicksave or quickload messages.
        /// </remarks>
        /// <param name="content">Message content.</param>
        /// <param name="duration">Duration before the message disappears, in seconds.</param>
        /// <param name="position">Position to display the message.</param>
        [KRPCProcedure]
        public static void Message (string content, float duration = 1f, MessagePosition position = MessagePosition.TopCenter)
        {
            ScreenMessages.PostScreenMessage (content, duration, position.ToScreenMessageStyle ());
        }

        /// <summary>
        /// Remove all user interface elements.
        /// </summary>
        /// <param name="clientOnly">If true, only remove objects created by the calling client.</param>
        [KRPCProcedure]
        public static void Clear (bool clientOnly = false)
        {
            if (clientOnly)
                Addon.Clear (KRPCCore.Context.RPCClient);
            else
                Addon.Clear ();
        }
    }
}
