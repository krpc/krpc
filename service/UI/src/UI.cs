using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.UI.ExtensionMethods;

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
        /// Add a new canvas.
        /// </summary>
        /// <remarks>
        /// If you want to add UI elements to KSPs stock UI canvas, use <see cref="StockCanvas"/>.
        /// </remarks>
        [KRPCProcedure]
        public static Canvas AddCanvas ()
        {
            return new Canvas ();
        }

        /// <summary>
        /// The stock UI canvas.
        /// </summary>
        [KRPCProperty]
        public static Canvas StockCanvas {
            get { return Canvas.StockCanvas; }
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
                Addon.Clear (CallContext.Client);
            else
                Addon.Clear ();
        }
    }
}
