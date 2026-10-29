using System;
using System.Collections.Generic;
using Brutal.ImGuiApi;
using KRPC.KSA.Services;
using KRPC.Server;
using KRPC.Service;
using KSA;

namespace KRPC.KSA.Utils
{
    /// <summary>
    /// The drawing objects, and the client that owns each. The game's gizmos and overlay are
    /// immediate mode, so the server draws every object each frame.
    /// </summary>
    public static class Drawables
    {
        sealed class Entry
        {
            public Drawable Object;
            public IClient Owner;
        }

        static readonly List<Entry> entries = new List<Entry> ();
        static bool subscribed;

        /// <summary>
        /// Add an object, owned by the calling client.
        /// </summary>
        internal static T Add<T> (T obj) where T : Drawable
        {
            if (!subscribed) {
                Core.Instance.OnClientDisconnected += (s, e) => RemoveAll (x => x.Owner == e.Client);
                subscribed = true;
            }
            entries.Add (new Entry { Object = obj, Owner = CallContext.Client });
            return obj;
        }

        internal static void Remove (Drawable obj)
        {
            RemoveAll (x => ReferenceEquals (x.Object, obj));
        }

        /// <summary>
        /// Remove every object, or only those owned by the calling client.
        /// </summary>
        internal static void Clear (bool clientOnly)
        {
            var client = CallContext.Client;
            RemoveAll (x => !clientOnly || x.Owner == client);
        }

        static void RemoveAll (Predicate<Entry> match)
        {
            var count = entries.RemoveAll (x => {
                if (!match (x))
                    return false;
                x.Object.Removed ();
                return true;
            });
            // Calls through the removed objects then raise an object destroyed exception
            if (count > 0)
                GameState.Sweep ();
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
            foreach (var entry in entries) {
                if (entry.Object.Visible)
                    Draw (() => entry.Object.DrawGizmos (gizmos, camera));
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
            foreach (var entry in entries) {
                if (entry.Object.Visible)
                    Draw (() => entry.Object.DrawOverlay (drawList, camera, viewport.Position));
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
