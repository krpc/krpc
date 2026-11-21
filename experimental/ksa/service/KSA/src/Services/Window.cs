using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A window, holding widgets one per line. Created using <see cref="UI.AddWindow"/>.
    /// </summary>
    /// <remarks>
    /// The window fits its widgets until its size is set. Closing the window makes it
    /// invisible. The window is hidden with the game's UI.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public sealed class Window : Container
    {
        string title;
        float2 position;
        float2 size;
        bool setPosition;
        bool setSize;
        bool fixedSize;

        internal Window (string title, float2 position, bool visible) : base (visible)
        {
            Title = title;
            this.position = position;
            setPosition = true;
        }

        /// <summary>
        /// The text in the title bar.
        /// </summary>
        [KRPCProperty]
        public string Title {
            get { return title; }
            set { title = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// The position of the top left corner of the window, from the top left corner of the
        /// game window, in pixels.
        /// </summary>
        [KRPCProperty]
        public Tuple<float, float> Position {
            get { return Tuple.Create (position.X, position.Y); }
            set {
                position = new float2 (value.Item1, value.Item2);
                setPosition = true;
            }
        }

        /// <summary>
        /// The width and height of the window, in pixels.
        /// </summary>
        /// <remarks>Throws an exception if the width or height is not positive.</remarks>
        [KRPCProperty]
        public Tuple<float, float> Size {
            get { return Tuple.Create (size.X, size.Y); }
            set {
                if (!(value.Item1 > 0) || !(value.Item2 > 0))
                    throw new ArgumentOutOfRangeException (nameof (Size));
                size = new float2 (value.Item1, value.Item2);
                setSize = true;
                fixedSize = true;
            }
        }

        internal override void Draw ()
        {
            if (!Visible)
                return;
            var origin = ImGui.GetMainViewport ().Pos;
            if (setPosition)
                ImGui.SetNextWindowPos (origin + position, ImGuiCond.Always);
            if (setSize)
                ImGui.SetNextWindowSize (size, ImGuiCond.Always);
            setPosition = false;
            setSize = false;
            var flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse;
            if (!fixedSize)
                flags |= ImGuiWindowFlags.AlwaysAutoResize;
            var open = true;
            try {
                if (ImGui.Begin (title + "###krpc-window-" + Id, ref open, flags)) {
                    ImGui.BeginDisabled (!Enabled);
                    DrawChildren (false);
                    ImGui.EndDisabled ();
                }
                position = ImGui.GetWindowPos () - origin;
                size = ImGui.GetWindowSize ();
            } finally {
                ImGui.End ();
            }
            if (!open)
                Visible = false;
        }

        internal override void DrawWidget ()
        {
        }
    }
}
