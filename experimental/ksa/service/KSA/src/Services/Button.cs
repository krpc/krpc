using System;
using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A button. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Button : Widget
    {
        string content;

        internal Button (string content, bool visible) : base (visible)
        {
            Content = content;
        }

        /// <summary>
        /// The text on the button.
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return content; }
            set { content = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// Whether the button has been clicked.
        /// </summary>
        /// <remarks>
        /// A click sets this to <c>true</c>, and it stays <c>true</c> until set to
        /// <c>false</c>.
        /// </remarks>
        [KRPCProperty]
        public bool Clicked { get; set; }

        internal override void DrawWidget ()
        {
            if (ImGui.Button (ItemLabel (content)))
                Clicked = true;
            RecordItem ();
        }
    }
}
