using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// Message position.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum MessagePosition
    {
        /// <summary>
        /// Bottom center.
        /// </summary>
        BottomCenter,
        /// <summary>
        /// Top center.
        /// </summary>
        TopCenter,
        /// <summary>
        /// Top left.
        /// </summary>
        TopLeft,
        /// <summary>
        /// Top right.
        /// </summary>
        TopRight
    }
}
