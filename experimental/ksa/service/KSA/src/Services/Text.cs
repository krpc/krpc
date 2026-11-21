using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// Text drawn over the scene, centered on a position. Created using <see cref="Drawing.AddText"/>.
    /// </summary>
    /// <remarks>
    /// The text is hidden with the game's UI.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Text : Drawable
    {
        double3 position;
        string content;
        float size;

        internal Text (string content, Tuple<double, double, double> position, float size, ReferenceFrame referenceFrame, bool visible) : base (referenceFrame, visible)
        {
            Content = content;
            Position = position;
            Size = size;
        }

        /// <summary>
        /// The text.
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return content; }
            set { content = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// The position of the center of the text, in meters.
        /// </summary>
        [KRPCProperty]
        public Tuple<double, double, double> Position {
            get { return Vectors.ToTuple (position); }
            set { position = Vectors.ToVector (value); }
        }

        /// <summary>
        /// The height of the font, in pixels.
        /// </summary>
        [KRPCProperty]
        public float Size {
            get { return size; }
            set {
                if (!(value > 0))
                    throw new ArgumentOutOfRangeException (nameof (Size));
                size = value;
            }
        }

        internal override void DrawOverlay (ImDrawListPtr drawList, global::KSA.Camera camera, float2 origin)
        {
            if (content.Length == 0)
                return;
            // NaN when the position is behind the camera
            var screen = camera.EgoToScreen (ToEgo (camera, position));
            if (float.IsNaN (screen.X) || float.IsNaN (screen.Y))
                return;
            var extent = ImGui.CalcTextSize (content, false, -1f) * (size / ImGui.GetFontSize ());
            var corner = origin + screen - extent * 0.5f;
            var shadow = corner + new float2 (1f, 1f);
            var font = ImGui.GetFont ();
            // A shadow, as the game's labels have
            drawList.AddText (font, size, in shadow, (ImColor8)new float4 (0f, 0f, 0f, 1f), content);
            drawList.AddText (font, size, in corner, (ImColor8)GizmoColor, content);
        }
    }
}
