using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// How the background of a panel is drawn.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum PanelStyle
    {
        /// <summary>
        /// As a window, the heavier of the two frames the game uses.
        /// </summary>
        Window,
        /// <summary>
        /// As a box, the lighter frame the game groups related controls in.
        /// </summary>
        Box,
        /// <summary>
        /// Not drawn at all, leaving whatever is behind the panel showing through.
        /// </summary>
        None
    }
}
