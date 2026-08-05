using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A container for user interface elements.
    /// Added to a <see cref="Canvas" /> or another panel.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Panel : Container
    {
        internal Panel (GameObject parent, bool visible)
            : base (Widgets.Create (parent, "krpc.panel", 200, 200), visible)
        {
            Widgets.AddImage (GameObject, Widgets.Style (skin => skin.window));
        }
    }
}
