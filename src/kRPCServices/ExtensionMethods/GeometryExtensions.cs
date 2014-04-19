using System;
using UnityEngine;

namespace KRPCServices.ExtensionMethods
{
    public static class GeometryExtensions
    {
        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Schema.Geometry.Vector3 ToMessage (this Vector3d v)
        {
            return KRPC.Schema.Geometry.Vector3.CreateBuilder ()
                .SetX (v.x).SetY (v.y).SetZ (v.z).Build ();
        }

        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Schema.Geometry.Vector3 ToMessage (this Vector3 v)
        {
            return KRPC.Schema.Geometry.Vector3.CreateBuilder ()
                .SetX (v.x).SetY (v.y).SetZ (v.z).Build ();
        }

        /// <summary>
        /// Convert a protocol buffer vector to a Unity3d vector
        /// </summary>
        public static Vector3d ToVector (this KRPC.Schema.Geometry.Vector3 v)
        {
            return new Vector3d (v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Compute the sign of a vector's elements
        /// </summary>
        public static Vector3d Sign (this Vector3d v)
        {
            return new Vector3d (Math.Sign (v.x), Math.Sign (v.y), Math.Sign (v.z));
        }

        /// <summary>
        /// Raise the elements of a vector's elements to the given exponent
        /// </summary>
        public static Vector3d Pow (this Vector3d v, double e)
        {
            return new Vector3d (Math.Pow (v.x, e), Math.Pow (v.y, e), Math.Pow (v.z, e));
        }

        /// <summary>
        /// Compute the element-wise inverse of a vector
        /// </summary>
        public static Vector3d Inverse (this Vector3d v)
        {
            return new Vector3d (1d / v.x, 1d / v.y, 1d / v.z);
        }

        /// <summary>
        /// Normalize an angle
        /// </summary>
        public static double normAngle (double angle)
        {
            return angle - 360d * Math.Floor ((angle + 180d) / 360d);
        }

        /// <summary>
        /// Apply normAngle element-wise to a vector
        /// </summary>
        public static Vector3d ReduceAngles (this Vector3d v)
        {
            return new Vector3d (normAngle (v.x), normAngle (v.y), normAngle (v.z));
        }

        /// <summary>
        /// Clamp a value to the given range
        /// </summary>
        public static T Clamp<T> (this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo (min) < 0)
                return min;
            else if (value.CompareTo (max) > 0)
                return max;
            else
                return value;
        }
    }
}

