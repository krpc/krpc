using System;
using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A dropdown list of options. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Dropdown : Widget
    {
        string label;
        IList<string> options;
        int selectedIndex = -1;
        readonly List<float2> optionCenters = new List<float2> ();

        internal Dropdown (string label, IList<string> options, bool visible) : base (visible)
        {
            Label = label;
            Options = options;
        }

        /// <summary>
        /// The text shown next to the dropdown.
        /// </summary>
        [KRPCProperty]
        public string Label {
            get { return label; }
            set { label = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// The options to choose from.
        /// </summary>
        /// <remarks>
        /// Setting the options clears the selection if the selected index is no longer in
        /// the list.
        /// </remarks>
        [KRPCProperty]
        public IList<string> Options {
            get { return new List<string> (options); }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (value));
                foreach (var option in value) {
                    if (option == null)
                        throw new ArgumentNullException (nameof (value));
                }
                options = new List<string> (value);
                if (selectedIndex >= options.Count)
                    selectedIndex = -1;
            }
        }

        /// <summary>
        /// The index of the selected option, or -1 for none.
        /// </summary>
        /// <remarks>Throws an exception if the index is out of range.</remarks>
        [KRPCProperty]
        public int SelectedIndex {
            get { return selectedIndex; }
            set {
                if (value < -1 || value >= options.Count)
                    throw new ArgumentOutOfRangeException (nameof (SelectedIndex));
                selectedIndex = value;
            }
        }

        /// <summary>
        /// Whether an option has been chosen from the list.
        /// </summary>
        /// <remarks>
        /// A choice sets this to <c>true</c>, and it stays <c>true</c> until set to
        /// <c>false</c>. Setting <see cref="SelectedIndex"/> leaves it unchanged.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }

        /// <summary>
        /// The screen positions of the centers of the options while the list is open, in
        /// pixels, and empty while it is closed. Used by the in-game tests to click them.
        /// </summary>
        public IList<float2> OptionCenters {
            get { return optionCenters; }
        }

        internal override void DrawWidget ()
        {
            optionCenters.Clear ();
            var preview = selectedIndex >= 0 ? options [selectedIndex] : string.Empty;
            // The open list replaces the last item
            if (!ImGui.BeginCombo (ItemLabel (label), preview)) {
                RecordItem ();
                return;
            }
            for (int i = 0; i < options.Count; i++) {
                ImGui.PushID (i);
                if (ImGui.Selectable (options [i], i == selectedIndex)) {
                    selectedIndex = i;
                    Changed = true;
                }
                optionCenters.Add ((ImGui.GetItemRectMin () + ImGui.GetItemRectMax ()) * 0.5f);
                ImGui.PopID ();
            }
            ImGui.EndCombo ();
        }
    }
}
