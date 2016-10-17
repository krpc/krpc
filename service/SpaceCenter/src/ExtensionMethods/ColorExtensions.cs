using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double, double, double>;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    /// <summary>
    /// Color extensions.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Convert the color to an RGB triple.
        /// </summary>
        public static Tuple3 ToTuple (this Color color)
        {
            return new Tuple3 (color.r, color.g, color.b);
        }

        /// <summary>
        /// Convert an RGB triple to a color.
        /// </summary>
        public static Color ToColor (this Tuple3 tuple)
        {
            return new Color ((float)tuple.Item1, (float)tuple.Item2, (float)tuple.Item3);
        }
    }
}
