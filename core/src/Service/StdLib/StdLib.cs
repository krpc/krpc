using System;
using KRPC.Service.Attributes;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// A standard library of utility functions, particularly useful within
    /// server side functions: scalar mathematics, and operations on vectors
    /// (as position/direction tuples) and quaternions (as rotation tuples, in
    /// the order x, y, z, w used throughout the SpaceCenter service).
    /// Angles are in radians unless stated otherwise.
    /// </summary>
    [KRPCService (Id = 8)]
    public static class StdLib
    {
        /// <summary>
        /// The constant pi.
        /// </summary>
        [KRPCProperty]
        public static double Pi {
            get { return Math.PI; }
        }

        /// <summary>
        /// The constant e, the base of the natural logarithm.
        /// </summary>
        [KRPCProperty]
        public static double E {
            get { return Math.E; }
        }

        /// <summary>
        /// The absolute value of a number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Abs (double x)
        {
            return Math.Abs (x);
        }

        /// <summary>
        /// The sign of a number: -1 if it is negative, 1 if it is positive,
        /// and 0 if it is zero.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static int Sign (double x)
        {
            return Math.Sign (x);
        }

        /// <summary>
        /// The largest whole number less than or equal to the given number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Floor (double x)
        {
            return Math.Floor (x);
        }

        /// <summary>
        /// The smallest whole number greater than or equal to the given number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Ceiling (double x)
        {
            return Math.Ceiling (x);
        }

        /// <summary>
        /// The given number rounded to the nearest whole number.
        /// Halfway values round away from zero.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Round (double x)
        {
            return Math.Round (x, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The square root of a number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Sqrt (double x)
        {
            return Math.Sqrt (x);
        }

        /// <summary>
        /// The sine of an angle, in radians.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Sin (double x)
        {
            return Math.Sin (x);
        }

        /// <summary>
        /// The cosine of an angle, in radians.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Cos (double x)
        {
            return Math.Cos (x);
        }

        /// <summary>
        /// The tangent of an angle, in radians.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Tan (double x)
        {
            return Math.Tan (x);
        }

        /// <summary>
        /// The angle, in radians, whose sine is the given number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Asin (double x)
        {
            return Math.Asin (x);
        }

        /// <summary>
        /// The angle, in radians, whose cosine is the given number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Acos (double x)
        {
            return Math.Acos (x);
        }

        /// <summary>
        /// The angle, in radians, whose tangent is the given number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Atan (double x)
        {
            return Math.Atan (x);
        }

        /// <summary>
        /// The angle, in radians, between the positive x axis and the point (x, y).
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Atan2 (double y, double x)
        {
            return Math.Atan2 (y, x);
        }

        /// <summary>
        /// The natural logarithm of a number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Log (double x)
        {
            return Math.Log (x);
        }

        /// <summary>
        /// The base 10 logarithm of a number.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Log10 (double x)
        {
            return Math.Log10 (x);
        }

        /// <summary>
        /// The number e raised to the given power.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double Exp (double x)
        {
            return Math.Exp (x);
        }

        /// <summary>
        /// The smaller of two numbers.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [KRPCProcedure]
        public static double Min (double x, double y)
        {
            return Math.Min (x, y);
        }

        /// <summary>
        /// The larger of two numbers.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        [KRPCProcedure]
        public static double Max (double x, double y)
        {
            return Math.Max (x, y);
        }

        /// <summary>
        /// The given number limited to the given range.
        /// </summary>
        /// <param name="x">The number.</param>
        /// <param name="min">The minimum of the range.</param>
        /// <param name="max">The maximum of the range.</param>
        [KRPCProcedure]
        public static double Clamp (double x, double min, double max)
        {
            return x < min ? min : (x > max ? max : x);
        }

        /// <summary>
        /// Convert an angle in degrees to radians.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double DegreesToRadians (double x)
        {
            return x * (Math.PI / 180.0);
        }

        /// <summary>
        /// Convert an angle in radians to degrees.
        /// </summary>
        /// <param name="x"></param>
        [KRPCProcedure]
        public static double RadiansToDegrees (double x)
        {
            return x * (180.0 / Math.PI);
        }

        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static Tuple3 VectorAdd (Tuple3 u, Tuple3 v)
        {
            return new Tuple3 (u.Item1 + v.Item1, u.Item2 + v.Item2, u.Item3 + v.Item3);
        }

        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static Tuple3 VectorSubtract (Tuple3 u, Tuple3 v)
        {
            return new Tuple3 (u.Item1 - v.Item1, u.Item2 - v.Item2, u.Item3 - v.Item3);
        }

        /// <summary>
        /// A vector scaled by a number.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <param name="scale">The scaling factor.</param>
        [KRPCProcedure]
        public static Tuple3 VectorScale (Tuple3 v, double scale)
        {
            return new Tuple3 (v.Item1 * scale, v.Item2 * scale, v.Item3 * scale);
        }

        /// <summary>
        /// The dot product of two vectors.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static double VectorDot (Tuple3 u, Tuple3 v)
        {
            return u.Item1 * v.Item1 + u.Item2 * v.Item2 + u.Item3 * v.Item3;
        }

        /// <summary>
        /// The cross product of two vectors.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static Tuple3 VectorCross (Tuple3 u, Tuple3 v)
        {
            return new Tuple3 (
                u.Item2 * v.Item3 - u.Item3 * v.Item2,
                u.Item3 * v.Item1 - u.Item1 * v.Item3,
                u.Item1 * v.Item2 - u.Item2 * v.Item1);
        }

        /// <summary>
        /// The magnitude (length) of a vector.
        /// </summary>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static double VectorMagnitude (Tuple3 v)
        {
            return Math.Sqrt (VectorDot (v, v));
        }

        /// <summary>
        /// A vector scaled to a magnitude of 1.
        /// </summary>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static Tuple3 VectorNormalize (Tuple3 v)
        {
            var magnitude = VectorMagnitude (v);
            if (magnitude <= 0)
                throw new ArgumentException ("Cannot normalize a zero vector");
            return VectorScale (v, 1.0 / magnitude);
        }

        /// <summary>
        /// The distance between two positions.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static double VectorDistance (Tuple3 u, Tuple3 v)
        {
            return VectorMagnitude (VectorSubtract (u, v));
        }

        /// <summary>
        /// The angle between two vectors, in radians, between 0 and pi.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        [KRPCProcedure]
        public static double VectorAngle (Tuple3 u, Tuple3 v)
        {
            var magnitudes = VectorMagnitude (u) * VectorMagnitude (v);
            if (magnitudes <= 0)
                throw new ArgumentException ("Cannot compute the angle of a zero vector");
            return Math.Acos (Clamp (VectorDot (u, v) / magnitudes, -1, 1));
        }

        /// <summary>
        /// Linear interpolation between two vectors.
        /// </summary>
        /// <param name="u">The vector when t is 0.</param>
        /// <param name="v">The vector when t is 1.</param>
        /// <param name="t">The interpolation parameter.</param>
        [KRPCProcedure]
        public static Tuple3 VectorLerp (Tuple3 u, Tuple3 v, double t)
        {
            return VectorAdd (VectorScale (u, 1 - t), VectorScale (v, t));
        }

        /// <summary>
        /// The composition of two rotations: the rotation r followed by the
        /// rotation q.
        /// </summary>
        /// <param name="q">The second rotation.</param>
        /// <param name="r">The first rotation.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionMultiply (Tuple4 q, Tuple4 r)
        {
            double qx = q.Item1, qy = q.Item2, qz = q.Item3, qw = q.Item4;
            double rx = r.Item1, ry = r.Item2, rz = r.Item3, rw = r.Item4;
            return new Tuple4 (
                qw * rx + qx * rw + qy * rz - qz * ry,
                qw * ry - qx * rz + qy * rw + qz * rx,
                qw * rz + qx * ry - qy * rx + qz * rw,
                qw * rw - qx * rx - qy * ry - qz * rz);
        }

        /// <summary>
        /// The inverse of a rotation.
        /// </summary>
        /// <param name="q"></param>
        [KRPCProcedure]
        public static Tuple4 QuaternionInverse (Tuple4 q)
        {
            var norm = q.Item1 * q.Item1 + q.Item2 * q.Item2 + q.Item3 * q.Item3 + q.Item4 * q.Item4;
            if (norm <= 0)
                throw new ArgumentException ("Cannot invert a zero quaternion");
            return new Tuple4 (-q.Item1 / norm, -q.Item2 / norm, -q.Item3 / norm, q.Item4 / norm);
        }

        /// <summary>
        /// A vector rotated by a rotation.
        /// </summary>
        /// <param name="q">The rotation.</param>
        /// <param name="v">The vector.</param>
        [KRPCProcedure]
        public static Tuple3 QuaternionRotateVector (Tuple4 q, Tuple3 v)
        {
            var axis = new Tuple3 (q.Item1, q.Item2, q.Item3);
            var t = VectorScale (VectorCross (axis, v), 2);
            return VectorAdd (
                VectorAdd (v, VectorScale (t, q.Item4)),
                VectorCross (axis, t));
        }

        /// <summary>
        /// The rotation about an axis by an angle.
        /// </summary>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation, in radians.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionFromAxisAngle (Tuple3 axis, double angle)
        {
            var unit = VectorNormalize (axis);
            var s = Math.Sin (angle / 2);
            return new Tuple4 (
                unit.Item1 * s, unit.Item2 * s, unit.Item3 * s, Math.Cos (angle / 2));
        }

        /// <summary>
        /// The angle of the rotation that takes one rotation to another,
        /// in radians, between 0 and pi.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        [KRPCProcedure]
        public static double QuaternionAngle (Tuple4 q, Tuple4 r)
        {
            var dot =
                q.Item1 * r.Item1 + q.Item2 * r.Item2 +
                q.Item3 * r.Item3 + q.Item4 * r.Item4;
            return 2 * Math.Acos (Clamp (Math.Abs (dot), 0, 1));
        }

        /// <summary>
        /// Spherical linear interpolation between two rotations.
        /// </summary>
        /// <param name="q">The rotation when t is 0.</param>
        /// <param name="r">The rotation when t is 1.</param>
        /// <param name="t">The interpolation parameter.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionSlerp (Tuple4 q, Tuple4 r, double t)
        {
            var dot = Clamp (
                q.Item1 * r.Item1 + q.Item2 * r.Item2 +
                q.Item3 * r.Item3 + q.Item4 * r.Item4, -1, 1);
            // Take the shorter path around the sphere
            var sign = 1.0;
            if (dot < 0) {
                dot = -dot;
                sign = -1.0;
            }
            double scale0, scale1;
            if (dot > 0.9995) {
                // The rotations are very close; interpolate linearly
                scale0 = 1 - t;
                scale1 = t;
            } else {
                var theta = Math.Acos (dot);
                var sinTheta = Math.Sin (theta);
                scale0 = Math.Sin ((1 - t) * theta) / sinTheta;
                scale1 = Math.Sin (t * theta) / sinTheta;
            }
            scale1 *= sign;
            var x = scale0 * q.Item1 + scale1 * r.Item1;
            var y = scale0 * q.Item2 + scale1 * r.Item2;
            var z = scale0 * q.Item3 + scale1 * r.Item3;
            var w = scale0 * q.Item4 + scale1 * r.Item4;
            var magnitude = Math.Sqrt (x * x + y * y + z * z + w * w);
            return new Tuple4 (x / magnitude, y / magnitude, z / magnitude, w / magnitude);
        }
    }
}
