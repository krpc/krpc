using System;
using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// Font style.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "UI")]
    public enum FontStyle
    {
        /// <summary>
        /// Normal.
        /// </summary>
        Normal,
        /// <summary>
        /// Bold.
        /// </summary>
        Bold,
        /// <summary>
        /// Italic.
        /// </summary>
        Italic,
        /// <summary>
        /// Bold and italic.
        /// </summary>
        BoldAndItalic
    }
}
