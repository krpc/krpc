using System;
using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A single line text input field. Added to a window or a row.
    /// </summary>
    /// <remarks>
    /// While the field is being edited, the game clears the rotation and translation inputs
    /// of the controlled vehicle each frame.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public sealed class InputField : Widget
    {
        // The most bytes of text the field holds, in UTF-8
        const int Capacity = 1024;
        string label;
        string value = string.Empty;

        internal InputField (string label, bool visible) : base (visible)
        {
            Label = label;
        }

        /// <summary>
        /// The text shown next to the field.
        /// </summary>
        [KRPCProperty]
        public string Label {
            get { return label; }
            set { label = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// The text in the field.
        /// </summary>
        [KRPCProperty]
        public string Value {
            get { return value; }
            set { this.value = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// Whether the text has been edited.
        /// </summary>
        /// <remarks>
        /// An edit sets this to <c>true</c>, and it stays <c>true</c> until set to
        /// <c>false</c>. Setting <see cref="Value"/> leaves it unchanged.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }

        internal override void DrawWidget ()
        {
            // ImGui holds the text being edited, and copies it into the buffer on each edit
            var buffer = new ImInputString (Capacity, value);
            if (ImGui.InputText (ItemLabel (label), buffer)) {
                value = buffer.ToString ();
                Changed = true;
            }
            RecordItem ();
        }
    }
}
