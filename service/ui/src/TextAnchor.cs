using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// Text alignment.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum TextAnchor
    {
        /// <summary>
        /// Lower center.
        /// </summary>
        LowerCenter,
        /// <summary>
        /// Lower left.
        /// </summary>
        LowerLeft,
        /// <summary>
        /// Lower right.
        /// </summary>
        LowerRight,
        /// <summary>
        /// Middle center.
        /// </summary>
        MiddleCenter,
        /// <summary>
        /// Middle left.
        /// </summary>
        MiddleLeft,
        /// <summary>
        /// Middle right.
        /// </summary>
        MiddleRight,
        /// <summary>
        /// Upper center.
        /// </summary>
        UpperCenter,
        /// <summary>
        /// Upper left.
        /// </summary>
        UpperLeft,
        /// <summary>
        /// Upper right.
        /// </summary>
        UpperRight
    }
}
