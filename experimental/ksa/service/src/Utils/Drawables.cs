using System;
using Brutal.ImGuiApi;
using KRPC.KSA.Services;
using KSA;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// The drawing objects. The game's gizmos and overlay are immediate mode, so the server
    /// draws every object each frame.
    /// </summary>
    public static class Drawables
    {
        static readonly ClientObjects<Drawable> objects = new ClientObjects<Drawable> (x => x.Removed ());

        /// <summary>
        /// Add an object, owned by the calling client.
        /// </summary>
        internal static T Add<T> (T obj) where T : Drawable
        {
            return objects.Add (obj);
        }

        internal static void Remove (Drawable obj)
        {
            objects.Remove (obj);
        }

        /// <summary>
        /// Remove every object, or only those owned by the calling client.
        /// </summary>
        internal static void Clear (bool clientOnly)
        {
            objects.Clear (clientOnly);
        }

        /// <summary>
        /// Draw the gizmos of the visible objects. Called by the server after the game resets
        /// the gizmos for the frame and before it renders them.
        /// </summary>
        public static void DrawGizmos ()
        {
            var camera = Program.MainViewport?.GetCamera ();
            var gizmos = Program.GizmosRenderer;
            if (camera == null || gizmos == null)
                return;
            for (int i = 0; i < objects.Count; i++) {
                var obj = objects [i];
                if (obj.Visible)
                    Draw (() => obj.DrawGizmos (gizmos, camera));
            }
        }

        /// <summary>
        /// Draw the overlay of the visible objects. Called by the server while the game draws
        /// its UI.
        /// </summary>
        public static void DrawOverlay ()
        {
            var viewport = Program.MainViewport;
            var camera = viewport?.GetCamera ();
            if (camera == null)
                return;
            var drawList = ImGui.GetBackgroundDrawList (ImGui.GetMainViewport ());
            for (int i = 0; i < objects.Count; i++) {
                var obj = objects [i];
                if (obj.Visible)
                    Draw (() => obj.DrawOverlay (drawList, camera, viewport.Position));
            }
        }

        static void Draw (Action draw)
        {
            try {
                draw ();
            } catch (Exception) {
                // An object in a frame the game cannot place this frame, such as one fixed to
                // a destroyed vehicle, is not drawn
            }
        }
    }
}
