using System;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The color of a part's highlight. See <see cref="Part.Highlight" />.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "KSA")]
    public enum HighlightColor
    {
        /// <summary>
        /// Not highlighted.
        /// </summary>
        None,
        /// <summary>
        /// Green.
        /// </summary>
        Green,
        /// <summary>
        /// Yellow.
        /// </summary>
        Yellow,
        /// <summary>
        /// Red.
        /// </summary>
        Red,
        /// <summary>
        /// Purple.
        /// </summary>
        Purple
    }
}
