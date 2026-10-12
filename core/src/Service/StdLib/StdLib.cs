using System;
using System.Globalization;
using KRPC.Service.Attributes;
using KRPC.Utils;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// A standard library of scalar mathematics, vector and quaternion operations, and
    /// printing, for use within server side functions.
    /// </summary>
    /// <remarks>
    /// Vectors are position and direction tuples, and rotations are unit quaternion tuples
    /// in the order x, y, z, w the SpaceCenter service uses. Angles are in radians unless
    /// stated otherwise.
    /// </remarks>
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
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Abs (double x)
        {
            return Math.Abs (x);
        }

        /// <summary>
        /// The sign of a number: -1 if it is negative, 1 if it is positive,
        /// and 0 if it is zero or not a number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static int Sign (double x)
        {
            return double.IsNaN (x) ? 0 : Math.Sign (x);
        }

        /// <summary>
        /// The magnitude of the first number with the sign of the second.
        /// </summary>
        /// <param name="x">The number giving the magnitude.</param>
        /// <param name="y">The number giving the sign.</param>
        [KRPCProcedure]
        public static double CopySign (double x, double y)
        {
            // The sign bit, which is also set for -0.0
            return BitConverter.DoubleToInt64Bits (y) < 0 ? -Math.Abs (x) : Math.Abs (x);
        }

        /// <summary>
        /// Whether a number is not a number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static bool IsNan (double x)
        {
            return double.IsNaN (x);
        }

        /// <summary>
        /// Whether a number is neither infinite nor not a number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static bool IsFinite (double x)
        {
            return !double.IsNaN (x) && !double.IsInfinity (x);
        }

        /// <summary>
        /// The largest whole number less than or equal to the given number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Floor (double x)
        {
            return Math.Floor (x);
        }

        /// <summary>
        /// The smallest whole number greater than or equal to the given number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Ceiling (double x)
        {
            return Math.Ceiling (x);
        }

        /// <summary>
        /// The given number rounded to the given number of decimal places,
        /// with a halfway value rounded to the nearer even number.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the rounded value is too large for a double.
        /// </remarks>
        /// <param name="x">The number.</param>
        /// <param name="digits">The number of decimal places to round to.
        /// Negative to round to tens, hundreds and beyond.</param>
        [KRPCProcedure]
        public static double Round (double x, int digits = 0)
        {
            return RoundToDigits (x, digits, MidpointRule.ToEven);
        }

        /// <summary>
        /// The given number rounded to the given number of decimal places,
        /// with a halfway value rounded away from zero.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the rounded value is too large for a double.
        /// </remarks>
        /// <param name="x">The number.</param>
        /// <param name="digits">The number of decimal places to round to.
        /// Negative to round to tens, hundreds and beyond.</param>
        [KRPCProcedure]
        public static double RoundHalfAwayFromZero (double x, int digits = 0)
        {
            return RoundToDigits (x, digits, MidpointRule.AwayFromZero);
        }

        /// <summary>
        /// The given number rounded to the given number of decimal places,
        /// with a halfway value rounded toward positive infinity.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the rounded value is too large for a double.
        /// </remarks>
        /// <param name="x">The number.</param>
        /// <param name="digits">The number of decimal places to round to.
        /// Negative to round to tens, hundreds and beyond.</param>
        [KRPCProcedure]
        public static double RoundHalfUp (double x, int digits = 0)
        {
            return RoundToDigits (x, digits, MidpointRule.Up);
        }

        /// <summary>
        /// Rounding to a number of decimal places, decided on the exact value of x, so a
        /// halfway value is one exactly halfway in decimal.
        /// </summary>
        static double RoundToDigits (double x, int digits, MidpointRule rule)
        {
            // Past 323 places, rounding moves x by less than half the gap between doubles
            if (double.IsNaN (x) || double.IsInfinity (x) || x == 0 || digits > 323)
                return x;
            if (digits < -308)
                return x < 0 ? -0.0 : 0.0;
            if (digits != 0)
                return DecimalRounding.Round (x, digits, rule);
            // A zero result keeps the sign of x
            return CopySign (RoundToWhole (x, rule), x);
        }

        static double RoundToWhole (double x, MidpointRule rule)
        {
            switch (rule) {
            case MidpointRule.ToEven:
                return Math.Round (x, MidpointRounding.ToEven);
            case MidpointRule.AwayFromZero:
                return Math.Round (x, MidpointRounding.AwayFromZero);
            default:
                // The fraction is compared with a half exactly, so the largest double
                // below a half rounds down
                var floor = Math.Floor (x);
                return x - floor >= 0.5 ? floor + 1 : floor;
            }
        }

        /// <summary>
        /// The square root of a number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Sqrt (double x)
        {
            return Math.Sqrt (x);
        }

        /// <summary>
        /// The length of the hypotenuse of a right triangle with the given sides.
        /// </summary>
        /// <param name="x">The first side.</param>
        /// <param name="y">The second side.</param>
        [KRPCProcedure]
        public static double Hypot (double x, double y)
        {
            x = Math.Abs (x);
            y = Math.Abs (y);
            if (double.IsInfinity (x) || double.IsInfinity (y))
                return double.PositiveInfinity;
            if (double.IsNaN (x) || double.IsNaN (y))
                return double.NaN;
            var max = Math.Max (x, y);
            if (max <= 0)
                return 0;
            var min = Math.Min (x, y);
            // Scaled by the larger side, so the squares do not overflow
            var ratio = min / max;
            return max * Math.Sqrt (1 + ratio * ratio);
        }

        /// <summary>
        /// The sine of an angle, in radians.
        /// </summary>
        /// <param name="x">The angle.</param>
        [KRPCProcedure]
        public static double Sin (double x)
        {
            return Math.Sin (x);
        }

        /// <summary>
        /// The cosine of an angle, in radians.
        /// </summary>
        /// <param name="x">The angle.</param>
        [KRPCProcedure]
        public static double Cos (double x)
        {
            return Math.Cos (x);
        }

        /// <summary>
        /// The tangent of an angle, in radians.
        /// </summary>
        /// <param name="x">The angle.</param>
        [KRPCProcedure]
        public static double Tan (double x)
        {
            return Math.Tan (x);
        }

        /// <summary>
        /// The angle, in radians, whose sine is the given number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Asin (double x)
        {
            return Math.Asin (x);
        }

        /// <summary>
        /// The angle, in radians, whose cosine is the given number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Acos (double x)
        {
            return Math.Acos (x);
        }

        /// <summary>
        /// The angle, in radians, whose tangent is the given number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Atan (double x)
        {
            return Math.Atan (x);
        }

        /// <summary>
        /// The angle, in radians, between the positive x axis and the point (x, y).
        /// </summary>
        /// <param name="y">The y coordinate.</param>
        /// <param name="x">The x coordinate.</param>
        [KRPCProcedure]
        public static double Atan2 (double y, double x)
        {
            return Math.Atan2 (y, x);
        }

        /// <summary>
        /// The logarithm of a number, natural unless another base is given.
        /// </summary>
        /// <param name="x">The number.</param>
        /// <param name="newBase">The base of the logarithm.</param>
        [KRPCProcedure]
        public static double Log (double x, double newBase = Math.E)
        {
            return Math.Log (x, newBase);
        }

        /// <summary>
        /// The base 10 logarithm of a number.
        /// </summary>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double Log10 (double x)
        {
            return Math.Log10 (x);
        }

        /// <summary>
        /// The number e raised to the given power.
        /// </summary>
        /// <param name="x">The power.</param>
        [KRPCProcedure]
        public static double Exp (double x)
        {
            return Math.Exp (x);
        }

        /// <summary>
        /// The smaller of two numbers.
        /// </summary>
        /// <param name="x">The first number.</param>
        /// <param name="y">The second number.</param>
        [KRPCProcedure]
        public static double Min (double x, double y)
        {
            return Math.Min (x, y);
        }

        /// <summary>
        /// The larger of two numbers.
        /// </summary>
        /// <param name="x">The first number.</param>
        /// <param name="y">The second number.</param>
        [KRPCProcedure]
        public static double Max (double x, double y)
        {
            return Math.Max (x, y);
        }

        /// <summary>
        /// The given number limited to the given range.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the minimum is greater than the maximum.
        /// </remarks>
        /// <param name="x">The number.</param>
        /// <param name="min">The minimum of the range.</param>
        /// <param name="max">The maximum of the range.</param>
        [KRPCProcedure]
        public static double Clamp (double x, double min, double max)
        {
            if (min > max)
                throw new ArgumentException (string.Format (
                    CultureInfo.InvariantCulture, "'{0}' cannot be greater than {1}.", min, max));
            return x < min ? min : (x > max ? max : x);
        }

        /// <summary>
        /// Linear interpolation between two numbers.
        /// </summary>
        /// <remarks>
        /// The parameter is not limited to the range 0 to 1, and a value outside it
        /// extrapolates.
        /// </remarks>
        /// <param name="a">The number when t is 0.</param>
        /// <param name="b">The number when t is 1.</param>
        /// <param name="t">The interpolation parameter.</param>
        [KRPCProcedure]
        public static double Lerp (double a, double b, double t)
        {
            // The difference of two finite numbers, or its product with t, can overflow for a
            // finite result. A product of the halves overflows only if the result does.
            if (IsFinite (a) && IsFinite (b) && IsFinite (t) && !IsFinite (LerpProduct (a, b, t)))
                return 2 * LerpFromNearerEnd (a / 2, b / 2, t);
            return LerpFromNearerEnd (a, b, t);
        }

        // From the nearer end, so the result is exact at t = 0 and t = 1
        static double LerpFromNearerEnd (double a, double b, double t)
        {
            return t < 0.5 ? a + LerpProduct (a, b, t) : b + LerpProduct (a, b, t);
        }

        static double LerpProduct (double a, double b, double t)
        {
            return t < 0.5 ? (b - a) * t : (a - b) * (1 - t);
        }

        /// <summary>
        /// The interpolation parameter of a number between two others,
        /// limited to the range 0 to 1.
        /// </summary>
        /// <remarks>
        /// Returns 0 if the two numbers are equal.
        /// </remarks>
        /// <param name="a">The number at 0.</param>
        /// <param name="b">The number at 1.</param>
        /// <param name="x">The number.</param>
        [KRPCProcedure]
        public static double InverseLerp (double a, double b, double x)
        {
            if (a == b)
                return 0;
            var difference = b - a;
            var offset = x - a;
            // The difference of two finite numbers can overflow, and that of their halves cannot.
            // Halving a subnormal number can round it to zero, so only an overflow halves.
            if (double.IsInfinity (difference) && !double.IsInfinity (a) && !double.IsInfinity (b)) {
                difference = b / 2 - a / 2;
                offset = x / 2 - a / 2;
            }
            return Clamp (offset / difference, 0, 1);
        }

        /// <summary>
        /// The angle in radians of an angle in degrees.
        /// </summary>
        /// <param name="x">The angle in degrees.</param>
        [KRPCProcedure]
        public static double DegreesToRadians (double x)
        {
            return x * (Math.PI / 180.0);
        }

        /// <summary>
        /// The angle in degrees of an angle in radians.
        /// </summary>
        /// <param name="x">The angle in radians.</param>
        [KRPCProcedure]
        public static double RadiansToDegrees (double x)
        {
            return x * (180.0 / Math.PI);
        }

        /// <summary>
        /// The sum of two vectors.
        /// </summary>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
        [KRPCProcedure]
        public static Tuple3 VectorAdd (Tuple3 u, Tuple3 v)
        {
            return new Tuple3 (u.Item1 + v.Item1, u.Item2 + v.Item2, u.Item3 + v.Item3);
        }

        /// <summary>
        /// The difference of two vectors.
        /// </summary>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
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
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
        [KRPCProcedure]
        public static double VectorDot (Tuple3 u, Tuple3 v)
        {
            return u.Item1 * v.Item1 + u.Item2 * v.Item2 + u.Item3 * v.Item3;
        }

        /// <summary>
        /// The cross product of two vectors.
        /// </summary>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
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
        /// <param name="v">The vector.</param>
        [KRPCProcedure]
        public static double VectorMagnitude (Tuple3 v)
        {
            return Hypot (Hypot (v.Item1, v.Item2), v.Item3);
        }

        /// <summary>
        /// A vector scaled to a magnitude of 1.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the vector is zero.
        /// </remarks>
        /// <param name="v">The vector.</param>
        [KRPCProcedure]
        public static Tuple3 VectorNormalize (Tuple3 v)
        {
            v = ScaleDown (v);
            var magnitude = VectorMagnitude (v);
            if (magnitude <= 0)
                throw new ArgumentException ("Cannot normalize a zero vector");
            return Divide (v, magnitude);
        }

        // Divided, as the reciprocal of a tiny magnitude overflows
        static Tuple3 Divide (Tuple3 v, double divisor)
        {
            return new Tuple3 (v.Item1 / divisor, v.Item2 / divisor, v.Item3 / divisor);
        }

        // The vector divided by its largest absolute component, so its magnitude is
        // between 1 and the square root of 3 and cannot overflow
        static Tuple3 ScaleDown (Tuple3 v)
        {
            var largest = Math.Max (Math.Max (Math.Abs (v.Item1), Math.Abs (v.Item2)), Math.Abs (v.Item3));
            return largest > 0 ? Divide (v, largest) : v;
        }

        /// <summary>
        /// The distance between two positions.
        /// </summary>
        /// <param name="u">The first position.</param>
        /// <param name="v">The second position.</param>
        [KRPCProcedure]
        public static double VectorDistance (Tuple3 u, Tuple3 v)
        {
            return VectorMagnitude (VectorSubtract (u, v));
        }

        /// <summary>
        /// The angle between two vectors, in radians, between 0 and pi.
        /// </summary>
        /// <remarks>
        /// Throws an exception if either vector is zero.
        /// </remarks>
        /// <param name="u">The first vector.</param>
        /// <param name="v">The second vector.</param>
        [KRPCProcedure]
        public static double VectorAngle (Tuple3 u, Tuple3 v)
        {
            u = ScaleDown (u);
            v = ScaleDown (v);
            if (VectorMagnitude (u) <= 0 || VectorMagnitude (v) <= 0)
                throw new ArgumentException ("Cannot compute the angle of a zero vector");
            // The arc tangent keeps its precision near 0 and pi, where an arc cosine does not
            return Math.Atan2 (VectorMagnitude (VectorCross (u, v)), VectorDot (u, v));
        }

        /// <summary>
        /// Linear interpolation between two vectors.
        /// </summary>
        /// <remarks>
        /// The parameter is not limited to the range 0 to 1, and a value outside it
        /// extrapolates.
        /// </remarks>
        /// <param name="u">The vector when t is 0.</param>
        /// <param name="v">The vector when t is 1.</param>
        /// <param name="t">The interpolation parameter.</param>
        [KRPCProcedure]
        public static Tuple3 VectorLerp (Tuple3 u, Tuple3 v, double t)
        {
            return new Tuple3 (Lerp (u.Item1, v.Item1, t), Lerp (u.Item2, v.Item2, t), Lerp (u.Item3, v.Item3, t));
        }

        /// <summary>
        /// The composition of two rotations: the rotation r followed by the
        /// rotation q.
        /// </summary>
        /// <remarks>
        /// Throws an exception if either quaternion is zero.
        /// </remarks>
        /// <param name="q">The second rotation.</param>
        /// <param name="r">The first rotation.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionMultiply (Tuple4 q, Tuple4 r)
        {
            CheckNotZero (q, "Cannot compose a zero quaternion");
            CheckNotZero (r, "Cannot compose a zero quaternion");
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
        /// <remarks>
        /// Throws an exception if the quaternion is zero.
        /// </remarks>
        /// <param name="q">The rotation.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionInverse (Tuple4 q)
        {
            CheckNotZero (q, "Cannot invert a zero quaternion");
            var largest = Math.Max (
                Math.Max (Math.Abs (q.Item1), Math.Abs (q.Item2)),
                Math.Max (Math.Abs (q.Item3), Math.Abs (q.Item4)));
            // Divided by the largest component first, so the squares cannot overflow or underflow
            double x = q.Item1 / largest, y = q.Item2 / largest, z = q.Item3 / largest, w = q.Item4 / largest;
            var sqrMagnitude = x * x + y * y + z * z + w * w;
            return new Tuple4 (
                -x / sqrMagnitude / largest, -y / sqrMagnitude / largest,
                -z / sqrMagnitude / largest, w / sqrMagnitude / largest);
        }

        /// <summary>
        /// A vector rotated by a rotation.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the quaternion is zero.
        /// </remarks>
        /// <param name="q">The rotation.</param>
        /// <param name="v">The vector.</param>
        [KRPCProcedure]
        public static Tuple3 QuaternionRotateVector (Tuple4 q, Tuple3 v)
        {
            CheckNotZero (q, "Cannot rotate by a zero quaternion");
            var axis = new Tuple3 (q.Item1, q.Item2, q.Item3);
            var t = VectorScale (VectorCross (axis, v), 2);
            return VectorAdd (
                VectorAdd (v, VectorScale (t, q.Item4)),
                VectorCross (axis, t));
        }

        /// <summary>
        /// The rotation about an axis by an angle.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the axis is zero.
        /// </remarks>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation, in radians.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionFromAxisAngle (Tuple3 axis, double angle)
        {
            var unit = ScaleDown (axis);
            var magnitude = VectorMagnitude (unit);
            if (magnitude <= 0)
                throw new ArgumentException ("The axis of rotation is a zero vector");
            unit = Divide (unit, magnitude);
            var s = Math.Sin (angle / 2);
            return new Tuple4 (
                unit.Item1 * s, unit.Item2 * s, unit.Item3 * s, Math.Cos (angle / 2));
        }

        /// <summary>
        /// The axis and angle of a rotation, with the angle in radians between 0 and pi.
        /// </summary>
        /// <remarks>
        /// The axis is a unit vector, or zero for no rotation. Throws an exception if the
        /// quaternion is zero.
        /// </remarks>
        /// <param name="q">The rotation.</param>
        [KRPCProcedure]
        public static Tuple<Tuple3, double> QuaternionToAxisAngle (Tuple4 q)
        {
            CheckNotZero (q, "Cannot compute the axis of a zero quaternion");
            var magnitude = Hypot (Hypot (q.Item1, q.Item2), Hypot (q.Item3, q.Item4));
            // q and -q are the same rotation, and the one with w >= 0 is the shorter way round
            var vector = Divide (new Tuple3 (q.Item1, q.Item2, q.Item3), q.Item4 < 0 ? -magnitude : magnitude);
            var sinHalf = VectorMagnitude (vector);
            if (sinHalf <= 0)
                return Tuple.Create (new Tuple3 (0, 0, 0), 0.0);
            var angle = 2 * Math.Atan2 (sinHalf, Math.Abs (q.Item4) / magnitude);
            return Tuple.Create (Divide (vector, sinHalf), angle);
        }

        /// <summary>
        /// The shortest rotation that takes one direction onto another.
        /// </summary>
        /// <remarks>
        /// Opposite directions give a half turn about an arbitrary perpendicular axis.
        /// Throws an exception if either vector is zero.
        /// </remarks>
        /// <param name="u">The direction to rotate from.</param>
        /// <param name="v">The direction to rotate to.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionFromToRotation (Tuple3 u, Tuple3 v)
        {
            u = VectorNormalize (u);
            v = VectorNormalize (v);
            var dot = VectorDot (u, v);
            var axis = VectorCross (u, v);
            // Within 1e-8 radians of opposite, rounding loses the axis, and a half turn
            // is as accurate
            if (dot < 0 && VectorMagnitude (axis) < 1e-8) {
                var other = Math.Abs (u.Item1) < 0.9 ? new Tuple3 (1, 0, 0) : new Tuple3 (0, 1, 0);
                var perpendicular = VectorNormalize (VectorCross (u, other));
                return new Tuple4 (perpendicular.Item1, perpendicular.Item2, perpendicular.Item3, 0);
            }
            var w = 1 + dot;
            var magnitude = Math.Sqrt (VectorDot (axis, axis) + w * w);
            return new Tuple4 (
                axis.Item1 / magnitude, axis.Item2 / magnitude, axis.Item3 / magnitude, w / magnitude);
        }

        /// <summary>
        /// The rotation that takes the z axis onto a forward direction, and the y axis
        /// toward an up direction.
        /// </summary>
        /// <remarks>
        /// The up direction need not be perpendicular to the forward direction. Throws an
        /// exception if either vector is zero, or if they are parallel.
        /// </remarks>
        /// <param name="forward">The direction of the z axis.</param>
        /// <param name="up">The direction the y axis points toward.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionLookRotation (Tuple3 forward, Tuple3 up)
        {
            forward = VectorNormalize (forward);
            up = VectorNormalize (up);
            up = VectorSubtract (up, VectorScale (forward, VectorDot (up, forward)));
            if (VectorMagnitude (up) < 1e-10)
                throw new ArgumentException ("The forward and up directions are parallel");
            up = VectorNormalize (up);
            var right = VectorCross (up, forward);
            // Shepperd's method, from the largest of the four squared components, so
            // the square root is of a value of at least 1
            double tw = 1 + right.Item1 + up.Item2 + forward.Item3;
            double tx = 1 + right.Item1 - up.Item2 - forward.Item3;
            double ty = 1 - right.Item1 + up.Item2 - forward.Item3;
            double tz = 1 - right.Item1 - up.Item2 + forward.Item3;
            double x, y, z, w;
            if (tw >= tx && tw >= ty && tw >= tz) {
                w = Math.Sqrt (tw) / 2;
                var r = 0.25 / w;
                x = (up.Item3 - forward.Item2) * r;
                y = (forward.Item1 - right.Item3) * r;
                z = (right.Item2 - up.Item1) * r;
            } else if (tx >= ty && tx >= tz) {
                x = Math.Sqrt (tx) / 2;
                var r = 0.25 / x;
                y = (right.Item2 + up.Item1) * r;
                z = (right.Item3 + forward.Item1) * r;
                w = (up.Item3 - forward.Item2) * r;
            } else if (ty >= tz) {
                y = Math.Sqrt (ty) / 2;
                var r = 0.25 / y;
                x = (right.Item2 + up.Item1) * r;
                z = (up.Item3 + forward.Item2) * r;
                w = (forward.Item1 - right.Item3) * r;
            } else {
                z = Math.Sqrt (tz) / 2;
                var r = 0.25 / z;
                x = (right.Item3 + forward.Item1) * r;
                y = (up.Item3 + forward.Item2) * r;
                w = (right.Item2 - up.Item1) * r;
            }
            return new Tuple4 (x, y, z, w);
        }

        /// <summary>
        /// The angle of the rotation that takes one rotation to another,
        /// in radians, between 0 and pi.
        /// </summary>
        /// <remarks>
        /// Throws an exception if either quaternion is zero.
        /// </remarks>
        /// <param name="q">The first rotation.</param>
        /// <param name="r">The second rotation.</param>
        [KRPCProcedure]
        public static double QuaternionAngle (Tuple4 q, Tuple4 r)
        {
            CheckNotZero (q, "Cannot compute the angle of a zero quaternion");
            CheckNotZero (r, "Cannot compute the angle of a zero quaternion");
            // The rotation from q to r, with the conjugate as the inverse of a unit quaternion
            var difference = QuaternionMultiply (new Tuple4 (-q.Item1, -q.Item2, -q.Item3, q.Item4), r);
            var sinHalf = Hypot (Hypot (difference.Item1, difference.Item2), difference.Item3);
            return 2 * Math.Atan2 (sinHalf, Math.Abs (difference.Item4));
        }

        /// <summary>
        /// Spherical linear interpolation between two rotations.
        /// </summary>
        /// <remarks>
        /// The parameter is not limited to the range 0 to 1, and a value outside it
        /// extrapolates. Throws an exception if either quaternion is zero.
        /// </remarks>
        /// <param name="q">The rotation when t is 0.</param>
        /// <param name="r">The rotation when t is 1.</param>
        /// <param name="t">The interpolation parameter.</param>
        [KRPCProcedure]
        public static Tuple4 QuaternionSlerp (Tuple4 q, Tuple4 r, double t)
        {
            CheckNotZero (q, "Cannot interpolate a zero quaternion");
            CheckNotZero (r, "Cannot interpolate a zero quaternion");
            // Take the shorter path around the sphere
            if (q.Item1 * r.Item1 + q.Item2 * r.Item2 + q.Item3 * r.Item3 + q.Item4 * r.Item4 < 0)
                r = new Tuple4 (-r.Item1, -r.Item2, -r.Item3, -r.Item4);
            // The angle between the two from the lengths of their difference and sum,
            // which keep their precision for a small angle where an arc cosine does not
            var difference = Hypot (
                Hypot (q.Item1 - r.Item1, q.Item2 - r.Item2), Hypot (q.Item3 - r.Item3, q.Item4 - r.Item4));
            var sum = Hypot (
                Hypot (q.Item1 + r.Item1, q.Item2 + r.Item2), Hypot (q.Item3 + r.Item3, q.Item4 + r.Item4));
            var theta = 2 * Math.Atan2 (difference, sum);
            double scale0, scale1;
            if (theta < 1e-8) {
                // A linear blend differs from the arc by the cube of the angle, below the
                // precision of a double
                scale0 = 1 - t;
                scale1 = t;
            } else {
                var sinTheta = Math.Sin (theta);
                scale0 = Math.Sin ((1 - t) * theta) / sinTheta;
                scale1 = Math.Sin (t * theta) / sinTheta;
            }
            var x = scale0 * q.Item1 + scale1 * r.Item1;
            var y = scale0 * q.Item2 + scale1 * r.Item2;
            var z = scale0 * q.Item3 + scale1 * r.Item3;
            var w = scale0 * q.Item4 + scale1 * r.Item4;
            var magnitude = Math.Sqrt (x * x + y * y + z * z + w * w);
            return new Tuple4 (x / magnitude, y / magnitude, z / magnitude, w / magnitude);
        }

        static void CheckNotZero (Tuple4 q, string message)
        {
            if (q.Item1 == 0 && q.Item2 == 0 && q.Item3 == 0 && q.Item4 == 0)
                throw new ArgumentException (message);
        }

        /// <summary>
        /// Writes a message to the game's log, for debugging.
        /// </summary>
        /// <remarks>
        /// A function evaluated by a stream or an event prints on every update. The message is
        /// written at the Info level, so a server log level of Warning or Error hides it.
        /// </remarks>
        /// <param name="message">The message.</param>
        [KRPCProcedure]
        public static void Print (string message)
        {
            Logger.WriteLine (message);
        }
    }
}
