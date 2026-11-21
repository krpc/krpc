using Brutal.Numerics;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Adds windows of buttons, text fields and other widgets to the game's UI.
    /// </summary>
    /// <remarks>
    /// The windows are removed when the client that added them disconnects. For messages on
    /// the screen, see <see cref="KSA.ShowMessage"/>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public sealed class UI
    {
        // The class holds static methods only
        UI ()
        {
        }

        /// <summary>
        /// Add a window, with no widgets.
        /// </summary>
        /// <param name="title">The text in the title bar.</param>
        /// <param name="visible">Whether the window is visible.</param>
        [KRPCMethod]
        public static Window AddWindow (string title, bool visible = true)
        {
            // Each new window opens below and to the right of the last
            var offset = 40 + 30 * (Utils.Windows.Count % 10);
            return Utils.Windows.Add (new Window (title, new float2 (offset, offset), visible));
        }

        /// <summary>
        /// Remove the windows.
        /// </summary>
        /// <param name="clientOnly">Whether to remove only the windows added by the calling client.</param>
        [KRPCMethod]
        public static void Clear (bool clientOnly = false)
        {
            Utils.Windows.Clear (clientOnly);
        }
    }
}
