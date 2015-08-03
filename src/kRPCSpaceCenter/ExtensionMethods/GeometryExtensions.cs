using System;
using UnityEngine;
using Tuple3 = KRPC.Utils.Tuple<double,double,double>;
using Tuple4 = KRPC.Utils.Tuple<double,double,double,double>;

namespace KRPCSpaceCenter.ExtensionMethods
{
    static class GeometryExtensions
    {
        /// <summary>
        /// Convert a vector to a tuple
        /// </summary>
        public static Tuple3 ToTuple (this Vector3d v)
        {
            return new Tuple3 (v.x, v.y, v.z);
        }

        /// <summary>
        /// Convert a tuple to a vector
        /// </summary>
        public static Vector3d ToVector (this Tuple3 t)
        {
            return new Vector3d (t.Item1, t.Item2, t.Item3);
        }

        /// <summary>
        /// Convert a quaternion to tuple
        /// </summary>
        public static Tuple4 ToTuple (this QuaternionD q)
        {
            return new Tuple4 (q.x, q.y, q.z, q.w);
        }

        /// <summary>
        /// Convert a tuple to a quaternion
        /// </summary>
        public static QuaternionD ToQuaternion (this Tuple4 t)
        {
            return new QuaternionD (t.Item1, t.Item2, t.Item3, t.Item4);
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
        /// Swap the Y and Z components of a vector
        /// </summary>
        public static Vector3d SwapYZ (this Vector3d v)
        {
            return new Vector3d (v.x, v.z, v.y);
        }

        /// <summary>
        /// Normalize an angle to the range (-180,180)
        /// </summary>
        public static double NormAngle (double angle)
        {
            return angle - 360d * Math.Floor ((angle + 180d) / 360d);
        }

        /// <summary>
        /// Apply NormAngle element-wise to a vector
        /// </summary>
        public static Vector3d ReduceAngles (this Vector3d v)
        {
            return new Vector3d (NormAngle (v.x), NormAngle (v.y), NormAngle (v.z));
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
        /// Clamp the elements of a vector to the given range
        /// </summary>
        public static Vector3 Clamp (this Vector3 v, float min, float max)
        {
            return new Vector3 (
                Mathf.Clamp (v.x, min, max),
                Mathf.Clamp (v.y, min, max),
                Mathf.Clamp (v.z, min, max));
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
        /// Clamps the given angle to the range [0,360]
        /// </summary>
        public static double ClampAngle360 (double angle)
        {
            angle = angle % 360d;
            if (angle < 0d)
                angle += 360d;
            return angle;
        }

        /// <summary>
        /// Clamps the given angle to the range [-180,180]
        /// </summary>
        public static double ClampAngle180 (double angle)
        {
            angle = ClampAngle360 (angle);
            if (angle > 180)
                angle -= 360;
            return angle;
        }

        public enum AxisOrder
        {
            YZX
        }

        /// <summary>
        /// Extract euler angles from a quarternion using the specified axis order.
        /// </summary>
        public static Vector3d EulerAngles (this QuaternionD q, AxisOrder order)
        {
            // Unity3d euler angle extraction order is ZXY
            Vector3d result;
            switch (order) {
            case AxisOrder.YZX:
                {
                    // FIXME: use double precision arithmetic
                    var angles = new Quaternion ((float)q.z, (float)q.x, (float)q.y, (float)q.w).eulerAngles;
                    result = new Vector3d (angles.z, angles.x, angles.y);
                    break;
                }
            default:
                throw new ArgumentException ("Axis order not supported");
            }
            // Clamp angles to range (0,360)
            result.x = ClampAngle360 (result.x);
            result.y = ClampAngle360 (result.y);
            result.z = ClampAngle360 (result.z);
            return result;
        }

        /// <summary>
        /// Compute the pitch, heading and roll angles of a quaternion in degrees.
        /// </summary>
        public static Vector3d PitchHeadingRoll (this QuaternionD q)
        {
            // Extract angles in order: roll (y), pitch (z), heading (x)
            Vector3d eulerAngles = q.EulerAngles (AxisOrder.YZX);
            var pitch = eulerAngles.y > 180d ? 360d - eulerAngles.y : -eulerAngles.y;
            var heading = eulerAngles.z;
            var roll = eulerAngles.x >= 90d ? 270d - eulerAngles.x : -90d - eulerAngles.x;
            return new Vector3d (pitch, heading, roll);
        }

        /// <summary>
        /// Create a quaternion rotation from the given euler angles, and axis order.
        /// </summary>
        public static QuaternionD QuaternionFromEuler (Vector3d eulerAngles, AxisOrder order)
        {
            QuaternionD result;
            switch (order) {
            case AxisOrder.YZX:
                // FIXME: use double precision arithmetic
                var angles = new Vector3 ((float)eulerAngles.y, (float)eulerAngles.z, (float)eulerAngles.x);
                var tmp = Quaternion.Euler (angles);
                result = new QuaternionD (tmp.y, tmp.z, tmp.x, tmp.w);
                break;
            default:
                throw new ArgumentException ("Axis order not supported");
            }
            return result;
        }

        /// <summary>
        /// Create a quarternion rotation for the given pitch, heading and roll angles.
        /// </summary>
        public static QuaternionD QuaternionFromPitchHeadingRoll (Vector3d phr)
        {
            var pitch = phr.x;
            var heading = phr.y;
            var roll = phr.z;
            var y = pitch < 0d ? -pitch : 360d - pitch;
            var z = heading;
            var x = roll <= -90d ? -roll - 90d : 270d - roll;
            return QuaternionFromEuler (new Vector3d (x, y, z), AxisOrder.YZX);
        }

        /// <summary>
        /// Compute the inverse quaternion. Assumes the input is a unit quaternion.
        /// </summary>
        public static QuaternionD Inverse (this QuaternionD q)
        {
            return new QuaternionD (-q.x, -q.y, -q.z, q.w);
        }

        /// <summary>
        /// Implementation of QuaternionD.OrthoNormalize, using stabilized Gram-Schmidt
        /// </summary>
        public static void OrthoNormalize2 (ref Vector3d normal, ref Vector3d tangent)
        {
            normal.Normalize ();
            tangent.Normalize (); // Additional normalization, avoids large tangent norm
            var proj = normal * Vector3d.Dot (tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize ();
        }

        /// <summary>
        /// Compute the norm of a quaternion
        /// </summary>
        public static double Norm (this QuaternionD q)
        {
            return Math.Sqrt (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        /// <summary>
        /// Normalize a quaternion
        /// </summary>
        public static QuaternionD Normalize (this QuaternionD q)
        {
            var sf = 1d / q.Norm ();
            return new QuaternionD (q.x * sf, q.y * sf, q.z * sf, q.w * sf);
        }

        /// <summary>
        /// Implementation of QuaternionD.LookRotation
        /// </summary>
        public static QuaternionD LookRotation2 (Vector3d forward, Vector3d up)
        {
            OrthoNormalize2 (ref forward, ref up);
            Vector3d right = Vector3d.Cross (up, forward);
            var w = Math.Sqrt (1.0d + right.x + up.y + forward.z) * 0.5d;
            var r = 0.25d / w;
            var x = (up.z - forward.y) * r;
            var y = (forward.x - right.z) * r;
            var z = (right.y - up.x) * r;
            return new QuaternionD (x, y, z, w);
        }

        /// <summary>
        /// Project a vector onto a plane, defined by its normal
        /// </summary>
        public static Vector3d ProjectVectorOntoPlane (Vector3d normal, Vector3d v)
        {
            return v - normal * Vector3d.Dot (normal, v);
        }
    }
}
