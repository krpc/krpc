using KRPC.Service.Attributes;

namespace KRPC.UI
{
    /// <summary>
    /// Text alignment.
    /// </summary>
    [KRPCEnum (Service = "UI")]
    public enum TextAlignment
    {
        /// <summary>
        /// Left aligned.
        /// </summary>
        Left,
        /// <summary>
        /// Right aligned.
        /// </summary>
        Right,
        /// <summary>
        /// Center aligned.
        /// </summary>
        Center
    }
}

