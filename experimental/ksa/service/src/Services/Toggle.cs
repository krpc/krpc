using System;
using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A check box with a label. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Toggle : Widget
    {
        string content;
        bool isChecked;

        internal Toggle (string content, bool visible) : base (visible)
        {
            Content = content;
        }

        /// <summary>
        /// The text shown next to the toggle.
        /// </summary>
        [KRPCProperty]
        public string Content {
            get { return content; }
            set { content = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// Whether the toggle is checked.
        /// </summary>
        [KRPCProperty]
        public bool Checked {
            get { return isChecked; }
            set { isChecked = value; }
        }

        /// <summary>
        /// Whether the toggle has been clicked.
        /// </summary>
        /// <remarks>
        /// A click sets this to <c>true</c>, and it stays <c>true</c> until set to
        /// <c>false</c>. Setting <see cref="Checked"/> leaves it unchanged.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }

        internal override void DrawWidget ()
        {
            if (ImGui.Checkbox (ItemLabel (content), ref isChecked))
                Changed = true;
            RecordItem ();
        }
    }
}
