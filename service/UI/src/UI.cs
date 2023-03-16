using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.UI.ExtensionMethods;
using UnityEngine;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.UI
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    static class CreateDefaultDuration
    {
        public static object Create()
        {
            return 1f;
        }
    }

    [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    static class CreateDefaultPosition
    {
        public static object Create()
        {
            return MessagePosition.TopCenter;
        }
    }

    [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
    static class CreateDefaultColor
    {
        public static object Create()
        {
            return new Tuple3(1, 0.92, 0.016);
        }
    }

    /// <summary>
    /// Provides functionality for drawing and interacting with in-game user interface elements.
    /// </summary>
    /// <remarks>
    /// For drawing 3D objects in the flight scene, see the Drawing service.
    /// </remarks>
    [KRPCService (Id = 7, GameScene = GameScene.All)]
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
        /// <param name="size">Size of the message, differs per position.</param>
        /// <param name="color">The color of the message.</param>
        [KRPCProcedure]
        [SuppressMessage("Gendarme.Rules.Globalization", "PreferIFormatProviderOverrideRule")]
        public static void Message(
            string content,
            [KRPCDefaultValue(typeof(CreateDefaultDuration))] float duration,
            [KRPCDefaultValue(typeof(CreateDefaultPosition))] MessagePosition position,
            [KRPCDefaultValue(typeof(CreateDefaultColor))] Tuple3 color,
            float size = 20)
        {
            var htmlColor = "#" + ColorUtility.ToHtmlStringRGB(color.ToColor());
            var message = "<color=" + htmlColor + "><size=" + size.ToString() + ">" + content + "</size></color>";
            ScreenMessages.PostScreenMessage(message, duration, position.ToScreenMessageStyle());
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
