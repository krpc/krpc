using KRPC.Service.StdLib;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Color extensions.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Convert the color to its red, green and blue components.
        /// </summary>
        public static ColorRGB ToColorRGB (this Color color)
        {
            return new ColorRGB (color.r, color.g, color.b);
        }

        /// <summary>
        /// Convert the color to its red, green, blue and alpha components.
        /// </summary>
        public static ColorRGBA ToColorRGBA (this Color color)
        {
            return new ColorRGBA (color.r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Convert red, green and blue components to an opaque color.
        /// </summary>
        public static Color ToColor (this ColorRGB color)
        {
            return new Color (color.R, color.G, color.B);
        }

        /// <summary>
        /// Convert red, green, blue and alpha components to a color.
        /// </summary>
        public static Color ToColor (this ColorRGBA color)
        {
            return new Color (color.R, color.G, color.B, color.A);
        }
    }
}
