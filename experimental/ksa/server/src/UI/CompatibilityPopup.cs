using System;
using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSA;

namespace KRPC.KSA.UI
{
    /// <summary>
    /// A modal warning that the running game is not the version the mod was built against,
    /// listing the RPCs found broken.
    /// </summary>
    sealed class CompatibilityPopup : Popup
    {
        const int MaxVisibleLines = 10;
        // The height grows to fit the body
        static readonly float2 size = new float2 (0.4f, 0.1f);

        readonly string versions;
        readonly IList<string> breakages;
        readonly PopupButton<CompatibilityPopup> [] buttons;

        /// <summary>
        /// Open the popup. Calls <paramref name="ignore"/> when the player chooses not to be
        /// shown it again for this game version.
        /// </summary>
        public CompatibilityPopup (Version modVersion, IList<string> breakages, Action ignore)
        {
            versions = "kRPC " + modVersion.ToString (3) + " was built for KSA " + GameCompatibility.BuiltFor +
                ". This is KSA " + GameCompatibility.Running + ".";
            this.breakages = breakages;
            buttons = new [] {
                CreateButton<CompatibilityPopup> ("OK", popup => popup.Active = false, PopupButtonKind.Primary),
                CreateButton<CompatibilityPopup> ("Don't show again for this version", popup => {
                    popup.Active = false;
                    ignore ();
                })
            };
        }

        protected override void OnDrawUi ()
        {
            if (!BeginConsoleModal (WindowId, "kRPC", size))
                return;
            ConsoleStyle.BeginBody ();
            ConsoleStyle.PushWidgetStyle ();
            CenteredWrappedText ("kRPC version not supported by this version of KSA");
            ImGui.Spacing ();
            ImGui.TextWrapped (versions);
            ImGui.TextWrapped ("kRPC APIs may break, fail silently or produce undefined behavior.");
            ImGui.Spacing ();
            if (breakages.Count == 0) {
                ImGui.Text ("Detected breakages: none");
            } else {
                ImGui.Text ("Detected breakages:");
                var lines = Math.Min (breakages.Count, MaxVisibleLines);
                float2? listSize = new float2 (0, lines * ImGui.GetTextLineHeightWithSpacing () + ImGui.GetTextLineHeight ());
                ImGui.BeginChild ("breakages", in listSize, ImGuiChildFlags.Borders);
                foreach (var breakage in breakages)
                    ImGui.Text (breakage);
                ImGui.EndChild ();
            }
            EndConsoleBody ();
            DrawConsoleButtonRow (this, buttons);
            EndConsoleModal ();
        }
    }
}
