using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The members shared by the windows and the widgets in them.
    /// </summary>
    public abstract class Widget : IGameObjectState
    {
        static int nextId;
        bool removed;
        Tuple<float2, float2> screenRect;
        int steadyFrames;

        /// <summary>
        /// Create a widget, enabled.
        /// </summary>
        protected Widget (bool visible)
        {
            Id = ++nextId;
            Visible = visible;
            Enabled = true;
        }

        /// <summary>
        /// The ImGui id of the widget, unique among the widgets.
        /// </summary>
        protected int Id { get; }

        /// <summary>
        /// The container the widget is in, or <c>null</c> for a window.
        /// </summary>
        internal Container Parent { get; set; }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return removed ? GameObjectState.Destroyed : GameObjectState.Live; }
        }

        /// <summary>
        /// Mark the widget as removed. Called when it leaves its container or the list of
        /// windows.
        /// </summary>
        internal virtual void Removed ()
        {
            removed = true;
        }

        /// <summary>
        /// Whether the widget is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible { get; set; }

        /// <summary>
        /// Whether the widget responds to input. A disabled widget is drawn dimmed.
        /// </summary>
        [KRPCProperty]
        public bool Enabled { get; set; }

        /// <summary>
        /// Remove the widget.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            if (Parent != null)
                Parent.Remove (this);
            else
                Utils.Windows.Remove ((Window)this);
        }

        /// <summary>
        /// The screen positions of the top left and bottom right corners of the widget, in
        /// pixels, once it has been drawn in the same place for a few frames, and <c>null</c>
        /// before. Used by the in-game tests to click it.
        /// </summary>
        public Tuple<float2, float2> ScreenRect {
            get { return steadyFrames >= 3 ? screenRect : null; }
        }

        /// <summary>
        /// Draw the widget for the current frame, if visible.
        /// </summary>
        internal virtual void Draw ()
        {
            if (!Visible)
                return;
            ImGui.PushID (Id);
            ImGui.BeginDisabled (!Enabled);
            DrawWidget ();
            ImGui.EndDisabled ();
            ImGui.PopID ();
        }

        /// <summary>
        /// Draw the widget's ImGui items.
        /// </summary>
        internal abstract void DrawWidget ();

        /// <summary>
        /// Record the screen position of the last ImGui item as the widget's.
        /// </summary>
        protected void RecordItem ()
        {
            // A new window measures its widgets on its first frame, then moves them
            var rect = Tuple.Create (ImGui.GetItemRectMin (), ImGui.GetItemRectMax ());
            steadyFrames = rect.Equals (screenRect) ? steadyFrames + 1 : 0;
            screenRect = rect;
        }

        /// <summary>
        /// An ImGui label that shows a text, with an id that does not change with it.
        /// </summary>
        protected static string ItemLabel (string text)
        {
            return text + "###item";
        }
    }
}
