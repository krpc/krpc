using KRPC.KSA.Services;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// The windows added by clients. ImGui is immediate mode, so the server draws every
    /// window each frame.
    /// </summary>
    public static class Windows
    {
        static readonly ClientObjects<Window> windows = new ClientObjects<Window> (x => x.Removed ());

        internal static int Count {
            get { return windows.Count; }
        }

        /// <summary>
        /// Add a window, owned by the calling client.
        /// </summary>
        internal static Window Add (Window window)
        {
            return windows.Add (window);
        }

        internal static void Remove (Window window)
        {
            windows.Remove (window);
        }

        /// <summary>
        /// Remove every window, or only those owned by the calling client.
        /// </summary>
        internal static void Clear (bool clientOnly)
        {
            windows.Clear (clientOnly);
        }

        /// <summary>
        /// Draw the visible windows. Called by the server while the game draws its UI.
        /// </summary>
        public static void Draw ()
        {
            for (int i = 0; i < windows.Count; i++)
                windows [i].Draw ();
        }
    }
}
