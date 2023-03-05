using System;
using UnityEngine;

namespace KRPC.Utils
{
    static class RectExtensions
    {
        internal static Rect ToRect (this Tuple<float,float,float,float> rect)
        {
            return new Rect (new Vector2 (rect.Item1, rect.Item2), new Vector2 (rect.Item3, rect.Item4));
        }

        internal static Tuple<float,float,float,float> ToTuple (this Rect rect)
        {
            return new Tuple<float,float,float,float> (rect.x, rect.y, rect.width, rect.height);
        }
    }
}
