using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A horizontal line between widgets. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Separator : Widget
    {
        internal Separator (bool visible) : base (visible)
        {
        }

        internal override void DrawWidget ()
        {
            ImGui.Separator ();
        }
    }
}
