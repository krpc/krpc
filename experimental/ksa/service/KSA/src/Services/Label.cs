using System;
using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A line of text. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Label : Widget
    {
        string content;

        internal Label (string content, bool visible) : base (visible)
        {
            Content = content;
        }

        /// <summary>
        /// The text of the label.
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return content; }
            set { content = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        internal override void DrawWidget ()
        {
            ImGui.Text (content);
            RecordItem ();
        }
    }
}
