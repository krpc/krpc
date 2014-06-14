using System;
using UnityEngine;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class GeometryExtensions
    {
        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Utils.Tuple<double,double,double> ToTuple (this Vector3d v)
        {
            return new KRPC.Utils.Tuple<double,double,double>(v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a Unity3d vector to a protocol buffer vector
        /// </summary>
        public static KRPC.Utils.Tuple<double,double,double> ToTuple (this Vector3 v)
        {
            return new KRPC.Utils.Tuple<double,double,double>(v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a protocol buffer vector to a Unity3d vector
        /// </summary>
        public static Vector3d ToVector (this KRPC.Utils.Tuple<double,double,double> v)
        {
            return new Vector3d (v.Item1, v.Item2, v.Item3);
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
        /// Round all values in a vector to the given precision
        /// </summary>
        public static Vector3 Round (this Vector3 v, int decimalPlaces)
        {
            // TODO: remove horrid casts
            return new Vector3 (
                (float)Math.Round ((double)v.x, decimalPlaces),
                (float)Math.Round ((double)v.y, decimalPlaces),
                (float)Math.Round ((double)v.z, decimalPlaces));
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

        /// <summary>
        /// Compute the pitch angle of a quaternion in degrees.
        /// </summary>
        public static Vector3 PitchHeadingRoll (this Quaternion q)
        {
            // First, adjust the euler angle extraction order frmo z,y,x -> -y,z,x
            // i.e. to extra pitch, then heading, then roll
            var r = q * Quaternion.Euler (-90f, 0f, 0f);
            // Convert angle around -y axis to pitch, with range [-90, 90]
            var pitch = r.eulerAngles.x > 180f ? r.eulerAngles.x - 360f : r.eulerAngles.x;
            // Convert angle around z axis to heading, with range [0, 360]
            var heading = 360f - r.eulerAngles.y;
            // Convert angle around x axis to heading, with range [-180, 180]
            var roll = 180f - r.eulerAngles.z;
            return new Vector3 (pitch, heading, roll).Round (1);
        }
    }
}
