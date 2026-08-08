using System;
using UnityEngine;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.UI.ExtensionMethods
{
    /// <summary>
    /// Color extensions. Colors in the user interface carry an alpha channel, as an
    /// element is drawn over whatever is behind it and often has to let some of it show
    /// through.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Convert the color to an RGBA tuple. Named apart from the RGB conversion the
        /// other services use, as a call to either would otherwise be ambiguous in a file
        /// that reaches for both.
        /// </summary>
        public static Tuple4 ToRgbaTuple (this Color color)
        {
            return new Tuple4 (color.r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Convert an RGBA tuple to a color.
        /// </summary>
        public static Color ToColor (this Tuple4 tuple)
        {
            if (tuple == null)
                throw new ArgumentNullException (nameof (tuple));
            return new Color (
                (float)tuple.Item1, (float)tuple.Item2,
                (float)tuple.Item3, (float)tuple.Item4);
        }
    }
}
