using System;
using System.Collections.Generic;
using KRPC.Service.Messages;
using KRPC.Utils;
using NUnit.Framework;
using Expression = KRPC.Service.KRPC.Expression;
using LinqExpression = System.Linq.Expressions.Expression;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Test.Service.StdLib
{
    using StdLib = global::KRPC.Service.StdLib.StdLib;

    [TestFixture]
    public class StdLibTest
    {
        static void AssertVectorsEqual (Tuple3 expected, Tuple3 actual, double delta = 1e-10)
        {
            Assert.AreEqual (expected.Item1, actual.Item1, delta);
            Assert.AreEqual (expected.Item2, actual.Item2, delta);
            Assert.AreEqual (expected.Item3, actual.Item3, delta);
        }

        static void AssertQuaternionsEqual (Tuple4 expected, Tuple4 actual, double delta = 1e-10)
        {
            // A quaternion and its negation represent the same rotation
            var sign = Math.Sign (expected.Item4) == Math.Sign (actual.Item4) ? 1 : -1;
            Assert.AreEqual (expected.Item1, sign * actual.Item1, delta);
            Assert.AreEqual (expected.Item2, sign * actual.Item2, delta);
            Assert.AreEqual (expected.Item3, sign * actual.Item3, delta);
            Assert.AreEqual (expected.Item4, sign * actual.Item4, delta);
        }

        [Test]
        public void Scalars ()
        {
            Assert.AreEqual (Math.PI, StdLib.Pi);
            Assert.AreEqual (Math.E, StdLib.E);
            Assert.AreEqual (4.2, StdLib.Abs (-4.2));
            Assert.AreEqual (-1, StdLib.Sign (-0.1));
            Assert.AreEqual (0, StdLib.Sign (double.NaN));
            Assert.AreEqual (-4, StdLib.Floor (-3.5));
            Assert.AreEqual (4, StdLib.Ceiling (3.5));
            Assert.AreEqual (3, StdLib.Sqrt (9));
            Assert.AreEqual (1, StdLib.Sin (Math.PI / 2), 1e-10);
            Assert.AreEqual (-1, StdLib.Cos (Math.PI), 1e-10);
            Assert.AreEqual (1, StdLib.Tan (Math.PI / 4), 1e-10);
            Assert.AreEqual (Math.PI / 2, StdLib.Asin (1), 1e-10);
            Assert.AreEqual (0, StdLib.Acos (1), 1e-10);
            Assert.AreEqual (Math.PI / 4, StdLib.Atan (1), 1e-10);
            Assert.AreEqual (Math.PI / 2, StdLib.Atan2 (1, 0), 1e-10);
            Assert.AreEqual (2, StdLib.Log10 (100), 1e-10);
            Assert.AreEqual (Math.E, StdLib.Exp (1), 1e-10);
            Assert.AreEqual (1, StdLib.Min (1, 2));
            Assert.AreEqual (2, StdLib.Max (1, 2));
            Assert.AreEqual (Math.PI, StdLib.DegreesToRadians (180), 1e-10);
            Assert.AreEqual (180, StdLib.RadiansToDegrees (Math.PI), 1e-10);
        }

        [Test]
        public void Logarithms ()
        {
            Assert.AreEqual (1, StdLib.Log (Math.E), 1e-10);
            Assert.AreEqual (3, StdLib.Log (8, 2), 1e-10);
            Assert.AreEqual (2, StdLib.Log (100, 10), 1e-10);
        }

        [Test]
        public void Clamp ()
        {
            Assert.AreEqual (5, StdLib.Clamp (7, 0, 5));
            Assert.AreEqual (0, StdLib.Clamp (-7, 0, 5));
            Assert.AreEqual (3, StdLib.Clamp (3, 0, 5));
            Assert.AreEqual (2, StdLib.Clamp (7, 2, 2));
            Assert.Throws<ArgumentException> (() => StdLib.Clamp (5, 10, 0));
        }

        [Test]
        public void Interpolation ()
        {
            Assert.AreEqual (3, StdLib.Lerp (2, 4, 0.5));
            Assert.AreEqual (6, StdLib.Lerp (2, 4, 2));
            Assert.AreEqual (0.25, StdLib.InverseLerp (2, 6, 3));
            Assert.AreEqual (0.25, StdLib.InverseLerp (6, 2, 5));
            Assert.AreEqual (1, StdLib.InverseLerp (2, 6, 9));
            Assert.AreEqual (0, StdLib.InverseLerp (2, 6, -9));
            Assert.AreEqual (0, StdLib.InverseLerp (2, 2, 2));
            // Exact at the ends, and no overflow over the whole range of doubles
            Assert.AreEqual (1, StdLib.Lerp (1e20, 1, 1));
            Assert.AreEqual (0.5, StdLib.InverseLerp (-1e308, 1e308, 0));
            Assert.AreEqual (0.7, StdLib.Lerp (0.1, 0.7, 1));
            Assert.AreEqual (1e308, StdLib.Lerp (1e308, 1e308, 2));
            Assert.AreEqual (-1e308, StdLib.Lerp (-1e308, 1e308, 0));
            Assert.AreEqual (0, StdLib.Lerp (-1e308, 1e308, 0.5));
            Assert.AreEqual (5e307, StdLib.Lerp (-1e308, 1e308, 0.75));
            Assert.AreEqual (1e308, StdLib.Lerp (-1e308, 1e308, 1));
            // Extrapolation whose difference times t overflows
            Assert.AreEqual (1.4e308, StdLib.Lerp (-1.5e308, -0.5e308, 2.9), 1e293);
            Assert.AreEqual (1.4e308, StdLib.Lerp (-0.5e308, -1.5e308, -1.9), 1e293);
            Assert.AreEqual (double.PositiveInfinity, StdLib.Lerp (-1e308, 1e308, 2));
            // A subnormal range
            Assert.AreEqual (1, StdLib.InverseLerp (0, 5e-324, 5e-324));
            Assert.AreEqual (0, StdLib.InverseLerp (0, 5e-324, 0));
            AssertVectorsEqual (new Tuple3 (0.7, 1e308, 1e308), StdLib.VectorLerp (
                new Tuple3 (0.1, 1e308, -1e308), new Tuple3 (0.7, 1e308, 1e308), 1), 0);
        }

        [Test]
        public void Signs ()
        {
            Assert.AreEqual (-3, StdLib.CopySign (3, -1));
            Assert.AreEqual (3, StdLib.CopySign (-3, 2));
            Assert.AreEqual (-3, StdLib.CopySign (3, -0.0));
            Assert.AreEqual (3, StdLib.CopySign (-3, 0.0));
        }

        [Test]
        public void Hypot ()
        {
            Assert.AreEqual (5, StdLib.Hypot (3, -4));
            Assert.AreEqual (0, StdLib.Hypot (0, 0));
            Assert.AreEqual (5e300, StdLib.Hypot (3e300, 4e300), 1e286);
            Assert.AreEqual (double.PositiveInfinity, StdLib.Hypot (double.NegativeInfinity, double.NaN));
            Assert.IsNaN (StdLib.Hypot (1, double.NaN));
        }

        [Test]
        public void NonFiniteValues ()
        {
            Assert.IsTrue (StdLib.IsNan (double.NaN));
            Assert.IsFalse (StdLib.IsNan (double.PositiveInfinity));
            Assert.IsTrue (StdLib.IsFinite (1.5));
            Assert.IsFalse (StdLib.IsFinite (double.NegativeInfinity));
            Assert.IsFalse (StdLib.IsFinite (double.NaN));
        }

        [Test]
        public void Rounding ()
        {
            // Each rule differs only in where it sends a halfway value
            Assert.AreEqual (2, StdLib.Round (2.5));
            Assert.AreEqual (-2, StdLib.Round (-2.5));
            Assert.AreEqual (3, StdLib.RoundHalfAwayFromZero (2.5));
            Assert.AreEqual (-3, StdLib.RoundHalfAwayFromZero (-2.5));
            Assert.AreEqual (3, StdLib.RoundHalfUp (2.5));
            Assert.AreEqual (-2, StdLib.RoundHalfUp (-2.5));
            // The largest double below a half, which adding a half rounds up to one
            Assert.AreEqual (0, StdLib.RoundHalfUp (0.49999999999999994));
            Assert.AreEqual (3, StdLib.Round (2.6));
            Assert.AreEqual (2, StdLib.Round (2.4));
            // A zero result keeps the sign of the value
            AssertNegativeZero (StdLib.Round (-0.4));
            AssertNegativeZero (StdLib.Round (-0.5));
            AssertNegativeZero (StdLib.RoundHalfAwayFromZero (-0.4));
            AssertNegativeZero (StdLib.RoundHalfUp (-0.4));
            AssertNegativeZero (StdLib.RoundHalfUp (-0.5));
            AssertNegativeZero (StdLib.RoundHalfUp (-0.004, 2));
        }

        static void AssertNegativeZero (double x)
        {
            Assert.AreEqual (0, x);
            Assert.IsTrue (BitConverter.DoubleToInt64Bits (x) < 0);
        }

        [Test]
        public void RoundingToDecimalPlaces ()
        {
            Assert.AreEqual (0.62, StdLib.Round (0.625, 2), 1e-10);
            Assert.AreEqual (0.63, StdLib.RoundHalfAwayFromZero (0.625, 2), 1e-10);
            Assert.AreEqual (0.63, StdLib.RoundHalfUp (0.625, 2), 1e-10);
            // The decimal written is not the double stored, and the stored value decides
            Assert.AreEqual (2.67, StdLib.Round (2.675, 2));
            Assert.AreEqual (2.67, StdLib.RoundHalfAwayFromZero (2.675, 2));
            Assert.AreEqual (2.67, StdLib.RoundHalfUp (2.675, 2));
            Assert.AreEqual (0.61, StdLib.Round (0.615, 2));
            Assert.AreEqual (1.11, StdLib.Round (1.115, 2));
            Assert.AreEqual (0.01, StdLib.Round (0.005, 2));
            // A double exactly halfway in decimal is a tie
            Assert.AreEqual (0.12, StdLib.Round (0.125, 2));
            Assert.AreEqual (0.13, StdLib.RoundHalfAwayFromZero (0.125, 2));
            Assert.AreEqual (-0.12, StdLib.RoundHalfUp (-0.125, 2));
            Assert.AreEqual (-0.13, StdLib.RoundHalfAwayFromZero (-0.125, 2));
            // Negative places round to tens and hundreds
            Assert.AreEqual (1200, StdLib.Round (1235, -2));
            Assert.AreEqual (1300, StdLib.RoundHalfAwayFromZero (1250, -2));
            Assert.AreEqual (1300, StdLib.RoundHalfUp (1250, -2));
            // A small value has digits to round beyond fifteen places
            Assert.AreEqual (1.2346e-12, StdLib.Round (1.23456789e-12, 16), 1e-24);
            Assert.AreEqual (1.2346e-12, StdLib.RoundHalfUp (1.23456789e-12, 16), 1e-24);
            // A value with no digits at that many places is unchanged
            Assert.AreEqual (1.5, StdLib.Round (1.5, 20));
            Assert.AreEqual (1.5, StdLib.Round (1.5, 400));
            // A subnormal value has digits up to 323 places
            Assert.AreEqual (0, StdLib.Round (1e-310, 309));
            Assert.AreEqual (2.2e-308, StdLib.Round (2.2250738585072014e-308, 309));
            Assert.AreEqual (1e-323, StdLib.Round (1.5e-323, 323));
            Assert.AreEqual (2e-323, StdLib.Round (2.5e-323, 323));
            Assert.AreEqual (5e-324, StdLib.Round (5e-324, 324));
            // A power of ten too large for a double rounds every finite value to zero
            Assert.AreEqual (0, StdLib.Round (1.5, -400));
            Assert.AreEqual (0, StdLib.RoundHalfUp (1.5, -400));
            Assert.AreEqual (0, StdLib.Round (2.5, int.MinValue));
            Assert.AreEqual (0, StdLib.RoundHalfUp (2.5, int.MinValue));
        }

        [Test]
        public void RoundingALargeValue ()
        {
            // A value this large has no fractional digits left to round
            Assert.AreEqual (1e300, StdLib.RoundHalfUp (1e300, 10));
            Assert.AreEqual (1e300, StdLib.Round (1e300, 10));
            Assert.AreEqual (9007199254740993.0, StdLib.RoundHalfUp (9007199254740993.0, 2));
            // A result too large for a double throws, and one too small is zero
            var exn = Assert.Throws<OverflowException> (() => StdLib.Round (1.7976931348623157e308, -308));
            Assert.AreEqual ("The rounded value is too large to represent", exn.Message);
            Assert.Throws<OverflowException> (() => StdLib.RoundHalfUp (-1.7976931348623157e308, -308));
            Assert.AreEqual (1.79769313e308, StdLib.Round (1.7976931348623157e308, -300));
            Assert.AreEqual (0, StdLib.Round (5e-324, 308));
        }

        [Test]
        public void RoundingANonFiniteValue ()
        {
            Assert.IsNaN (StdLib.Round (double.NaN, -400));
            Assert.IsNaN (StdLib.RoundHalfUp (double.NaN, 2));
            Assert.AreEqual (double.PositiveInfinity, StdLib.RoundHalfAwayFromZero (double.PositiveInfinity, -400));
            Assert.AreEqual (double.NegativeInfinity, StdLib.RoundHalfUp (double.NegativeInfinity, -2));
        }

        [Test]
        public void Vectors ()
        {
            var x = new Tuple3 (1, 0, 0);
            var y = new Tuple3 (0, 1, 0);
            var z = new Tuple3 (0, 0, 1);
            AssertVectorsEqual (new Tuple3 (1, 1, 0), StdLib.VectorAdd (x, y));
            AssertVectorsEqual (new Tuple3 (1, -1, 0), StdLib.VectorSubtract (x, y));
            AssertVectorsEqual (new Tuple3 (2.5, 0, 0), StdLib.VectorScale (x, 2.5));
            Assert.AreEqual (0, StdLib.VectorDot (x, y));
            AssertVectorsEqual (z, StdLib.VectorCross (x, y));
            Assert.AreEqual (5, StdLib.VectorMagnitude (new Tuple3 (3, 4, 0)));
            AssertVectorsEqual (new Tuple3 (0.6, 0.8, 0), StdLib.VectorNormalize (new Tuple3 (3, 4, 0)));
            Assert.AreEqual (5, StdLib.VectorDistance (new Tuple3 (1, 1, 0), new Tuple3 (4, 5, 0)));
            Assert.AreEqual (Math.PI / 2, StdLib.VectorAngle (x, y), 1e-10);
            AssertVectorsEqual (new Tuple3 (0.5, 0.5, 0), StdLib.VectorLerp (x, y, 0.5));
        }

        [Test]
        public void ZeroVectors ()
        {
            var zero = new Tuple3 (0, 0, 0);
            Assert.Throws<ArgumentException> (() => StdLib.VectorNormalize (zero));
            Assert.Throws<ArgumentException> (() => StdLib.VectorAngle (zero, new Tuple3 (1, 0, 0)));
            var exn = Assert.Throws<ArgumentException> (() => StdLib.QuaternionFromAxisAngle (zero, 1));
            Assert.AreEqual ("The axis of rotation is a zero vector", exn.Message);
        }

        [Test]
        public void ZeroQuaternions ()
        {
            var zero = new Tuple4 (0, 0, 0, 0);
            var identity = new Tuple4 (0, 0, 0, 1);
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionMultiply (zero, identity));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionMultiply (identity, zero));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionInverse (zero));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionRotateVector (zero, new Tuple3 (1, 0, 0)));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionToAxisAngle (zero));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionAngle (zero, identity));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionAngle (identity, zero));
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionSlerp (zero, identity, 0.5));
            var exn = Assert.Throws<ArgumentException> (() => StdLib.QuaternionSlerp (identity, zero, 0.5));
            Assert.AreEqual ("Cannot interpolate a zero quaternion", exn.Message);
        }

        [Test]
        public void LargeAndSmallVectors ()
        {
            foreach (var scale in new [] { 1e200, 1e-200 }) {
                var v = new Tuple3 (3 * scale, 0, 4 * scale);
                Assert.AreEqual (5 * scale, StdLib.VectorMagnitude (v), 5 * scale * 1e-15);
                AssertVectorsEqual (new Tuple3 (0.6, 0, 0.8), StdLib.VectorNormalize (v));
                Assert.AreEqual (Math.PI / 2, StdLib.VectorAngle (v, new Tuple3 (0, scale, 0)), 1e-10);
                var axisAngle = StdLib.QuaternionToAxisAngle (new Tuple4 (scale, 0, 0, scale));
                AssertVectorsEqual (new Tuple3 (1, 0, 0), axisAngle.Axis);
                Assert.AreEqual (Math.PI / 2, axisAngle.Angle, 1e-10);
            }
        }

        [Test]
        public void SmallAngles ()
        {
            var x = new Tuple3 (1, 0, 0);
            Assert.AreEqual (1e-10, StdLib.VectorAngle (x, new Tuple3 (1, 1e-10, 0)), 1e-20);
            Assert.AreEqual (Math.PI - 1e-10, StdLib.VectorAngle (x, new Tuple3 (-1, 1e-10, 0)), 1e-15);
            var identity = new Tuple4 (0, 0, 0, 1);
            var small = StdLib.QuaternionFromAxisAngle (new Tuple3 (0, 0, 1), 1e-10);
            Assert.AreEqual (1e-10, StdLib.QuaternionAngle (identity, small), 1e-20);
            Assert.AreEqual (1e-10, StdLib.QuaternionAngle (small, identity), 1e-20);
            var negated = new Tuple4 (-small.Item1, -small.Item2, -small.Item3, -small.Item4);
            Assert.AreEqual (1e-10, StdLib.QuaternionAngle (identity, negated), 1e-20);
        }

        [Test]
        public void SlerpBetweenCloseRotations ()
        {
            var identity = new Tuple4 (0, 0, 0, 1);
            var z = new Tuple3 (0, 0, 1);
            foreach (var angle in new [] { 0.5, 0.06, 1e-4, 1e-9 }) {
                var r = StdLib.QuaternionFromAxisAngle (z, angle);
                AssertQuaternionsEqual (
                    StdLib.QuaternionFromAxisAngle (z, angle / 4), StdLib.QuaternionSlerp (identity, r, 0.25), 1e-15);
                AssertQuaternionsEqual (
                    StdLib.QuaternionFromAxisAngle (z, angle * 1.5), StdLib.QuaternionSlerp (identity, r, 1.5), 1e-15);
                // The negation of a rotation is the same rotation
                var negated = new Tuple4 (-r.Item1, -r.Item2, -r.Item3, -r.Item4);
                AssertQuaternionsEqual (
                    StdLib.QuaternionFromAxisAngle (z, angle / 4), StdLib.QuaternionSlerp (identity, negated, 0.25), 1e-15);
            }
        }

        [Test]
        public void VectorsBeyondTheRangeOfAMagnitude ()
        {
            // The magnitude overflows, and the direction does not
            var huge = new Tuple3 (1.5e308, 1.5e308, 0);
            Assert.AreEqual (double.PositiveInfinity, StdLib.VectorMagnitude (huge));
            AssertVectorsEqual (new Tuple3 (Math.Sqrt (0.5), Math.Sqrt (0.5), 0), StdLib.VectorNormalize (huge));
            Assert.AreEqual (Math.PI / 4, StdLib.VectorAngle (huge, new Tuple3 (1, 0, 0)), 1e-15);
            AssertQuaternionsEqual (
                StdLib.QuaternionFromAxisAngle (new Tuple3 (1, 1, 0), 1),
                StdLib.QuaternionFromAxisAngle (huge, 1));
            // A quaternion whose squared magnitude overflows or underflows
            Assert.AreEqual (-1e170, StdLib.QuaternionInverse (new Tuple4 (1e-170, 0, 0, 0)).Item1, 1e155);
            Assert.AreEqual (-1e-170, StdLib.QuaternionInverse (new Tuple4 (1e170, 0, 0, 0)).Item1, 1e-185);
            // A rotation whose half angle has a sine with no finite reciprocal
            var axisAngle = StdLib.QuaternionToAxisAngle (new Tuple4 (1e-310, 0, 0, 1));
            AssertVectorsEqual (new Tuple3 (1, 0, 0), axisAngle.Axis);
            Assert.AreEqual (2e-310, axisAngle.Angle, 1e-320);
        }

        [Test]
        public void Quaternions ()
        {
            var identity = new Tuple4 (0, 0, 0, 1);
            var zAxis = new Tuple3 (0, 0, 1);
            // 90 degrees about the z axis
            var quarter = StdLib.QuaternionFromAxisAngle (zAxis, Math.PI / 2);
            AssertQuaternionsEqual (
                new Tuple4 (0, 0, Math.Sin (Math.PI / 4), Math.Cos (Math.PI / 4)), quarter);

            // Rotating the x axis 90 degrees about z gives the y axis
            AssertVectorsEqual (
                new Tuple3 (0, 1, 0), StdLib.QuaternionRotateVector (quarter, new Tuple3 (1, 0, 0)));

            // Composing two quarter turns gives a half turn
            var half = StdLib.QuaternionMultiply (quarter, quarter);
            AssertVectorsEqual (
                new Tuple3 (-1, 0, 0), StdLib.QuaternionRotateVector (half, new Tuple3 (1, 0, 0)));

            // A rotation composed with its inverse is the identity
            AssertQuaternionsEqual (
                identity, StdLib.QuaternionMultiply (quarter, StdLib.QuaternionInverse (quarter)));

            Assert.AreEqual (Math.PI / 2, StdLib.QuaternionAngle (identity, quarter), 1e-10);

            // Interpolating half way between the identity and a half turn
            // gives a quarter turn
            AssertQuaternionsEqual (quarter, StdLib.QuaternionSlerp (identity, half, 0.5));
        }

        [Test]
        public void QuaternionToAxisAngle ()
        {
            var axis = StdLib.VectorNormalize (new Tuple3 (1, 2, -2));
            var q = StdLib.QuaternionFromAxisAngle (axis, 1.2);
            var result = StdLib.QuaternionToAxisAngle (q);
            AssertVectorsEqual (axis, result.Axis);
            Assert.AreEqual (1.2, result.Angle, 1e-10);

            // The negated quaternion is the same rotation
            result = StdLib.QuaternionToAxisAngle (new Tuple4 (-q.Item1, -q.Item2, -q.Item3, -q.Item4));
            AssertVectorsEqual (axis, result.Axis);
            Assert.AreEqual (1.2, result.Angle, 1e-10);

            // A rotation past a half turn is the shorter one the other way round
            result = StdLib.QuaternionToAxisAngle (StdLib.QuaternionFromAxisAngle (axis, 1.5 * Math.PI));
            AssertVectorsEqual (StdLib.VectorScale (axis, -1), result.Axis);
            Assert.AreEqual (0.5 * Math.PI, result.Angle, 1e-10);

            // A quaternion that is not unit length
            result = StdLib.QuaternionToAxisAngle (new Tuple4 (0, 0, 2, 2));
            AssertVectorsEqual (new Tuple3 (0, 0, 1), result.Axis);
            Assert.AreEqual (Math.PI / 2, result.Angle, 1e-10);

            result = StdLib.QuaternionToAxisAngle (new Tuple4 (0, 0, 0, 1));
            AssertVectorsEqual (new Tuple3 (0, 0, 0), result.Axis);
            Assert.AreEqual (0, result.Angle);
        }

        [Test]
        public void QuaternionFromToRotation ()
        {
            var u = new Tuple3 (1, 2, 3);
            var v = new Tuple3 (-2, 0.5, 1);
            var q = StdLib.QuaternionFromToRotation (u, v);
            AssertVectorsEqual (StdLib.VectorNormalize (v), StdLib.VectorNormalize (StdLib.QuaternionRotateVector (q, u)));
            Assert.AreEqual (StdLib.VectorAngle (u, v), StdLib.QuaternionToAxisAngle (q).Angle, 1e-10);

            AssertQuaternionsEqual (new Tuple4 (0, 0, 0, 1), StdLib.QuaternionFromToRotation (u, StdLib.VectorScale (u, 2)));

            // Opposite directions give a half turn
            q = StdLib.QuaternionFromToRotation (u, StdLib.VectorScale (u, -1));
            AssertVectorsEqual (StdLib.VectorScale (StdLib.VectorNormalize (u), -1), StdLib.VectorNormalize (StdLib.QuaternionRotateVector (q, u)));
            q = StdLib.QuaternionFromToRotation (new Tuple3 (1, 0, 0), new Tuple3 (-1, 0, 0));
            AssertVectorsEqual (new Tuple3 (-1, 0, 0), StdLib.QuaternionRotateVector (q, new Tuple3 (1, 0, 0)));
            // Nearly opposite directions still rotate onto the target
            var nearlyOpposite = StdLib.VectorNormalize (new Tuple3 (-1, 1e-6, 0));
            q = StdLib.QuaternionFromToRotation (new Tuple3 (1, 0, 0), nearlyOpposite);
            AssertVectorsEqual (nearlyOpposite, StdLib.QuaternionRotateVector (q, new Tuple3 (1, 0, 0)), 1e-9);

            Assert.Throws<ArgumentException> (() => StdLib.QuaternionFromToRotation (u, new Tuple3 (0, 0, 0)));
        }

        [Test]
        public void QuaternionLookRotation ()
        {
            var x = new Tuple3 (1, 0, 0);
            var y = new Tuple3 (0, 1, 0);
            var z = new Tuple3 (0, 0, 1);
            AssertQuaternionsEqual (new Tuple4 (0, 0, 0, 1), StdLib.QuaternionLookRotation (z, y));
            AssertQuaternionsEqual (
                StdLib.QuaternionFromAxisAngle (y, Math.PI / 2), StdLib.QuaternionLookRotation (x, y));

            // The z axis onto forward, and the y axis onto up made perpendicular to it, in
            // cases taking each of the four branches of the conversion
            var cases = new [] {
                Tuple.Create (new Tuple3 (0, 0, 1), new Tuple3 (0, -1, 0)),
                Tuple.Create (new Tuple3 (1, 2, 3), new Tuple3 (0, 1, 0)),
                Tuple.Create (new Tuple3 (0, 0, -1), new Tuple3 (1, 1, 0)),
                Tuple.Create (new Tuple3 (1, 0, 0), new Tuple3 (0, 0, 1)),
                Tuple.Create (new Tuple3 (0, -1, 0.1), new Tuple3 (-1, 0, 0)),
                Tuple.Create (new Tuple3 (-1, 0, 0), new Tuple3 (0, -1, 0)),
            };
            foreach (var c in cases) {
                var forward = StdLib.VectorNormalize (c.Item1);
                var up = StdLib.VectorNormalize (StdLib.VectorSubtract (
                    c.Item2, StdLib.VectorScale (forward, StdLib.VectorDot (c.Item2, forward))));
                var q = StdLib.QuaternionLookRotation (c.Item1, c.Item2);
                AssertVectorsEqual (forward, StdLib.QuaternionRotateVector (q, z));
                AssertVectorsEqual (up, StdLib.QuaternionRotateVector (q, y));
                AssertVectorsEqual (StdLib.VectorCross (up, forward), StdLib.QuaternionRotateVector (q, x));
            }

            var exn = Assert.Throws<ArgumentException> (() => StdLib.QuaternionLookRotation (x, new Tuple3 (-2, 0, 0)));
            Assert.AreEqual ("The forward and up directions are parallel", exn.Message);
            Assert.Throws<ArgumentException> (() => StdLib.QuaternionLookRotation (new Tuple3 (0, 0, 0), y));
        }

        static T Eval<T> (Expression expression)
        {
            return LinqExpression.Lambda<Func<T>> (expression).Compile () ();
        }

        static Expression Call (string procedure, params Expression[] args)
        {
            var arguments = new Dictionary<int, Expression> ();
            for (int i = 0; i < args.Length; i++)
                arguments [i] = args [i];
            return Expression.CallWithArguments (new ProcedureCall ("StdLib", procedure), arguments);
        }

        static Expression Vector (int x, int y, int z)
        {
            return Expression.CreateTuple (new List<Expression> {
                Expression.ConstantInt (x), Expression.ConstantInt (y), Expression.ConstantInt (z)
            });
        }

        [Test]
        public void CallFromAFunction ()
        {
            // A tuple of ints passes as a vector
            Assert.AreEqual (5, Eval<double> (Call ("VectorMagnitude", Vector (3, 4, 0))));
            // An omitted argument takes its default
            Assert.AreEqual (2, Eval<double> (Call ("Round", Expression.ConstantDouble (2.5))));
            Assert.AreEqual (1, Eval<double> (Call ("Log", Expression.ConstantDouble (Math.E))), 1e-10);
            Assert.Throws<ArgumentException> (() => Eval<Tuple3> (Call ("VectorNormalize", Vector (0, 0, 0))));
        }

        [Test]
        public void Print ()
        {
            var messages = new List<string> ();
            var writer = Logger.Writer;
            var format = Logger.Format;
            try {
                Logger.Writer = (message, severity) => messages.Add (message);
                Logger.Format = "{2}";
                StdLib.Print ("hello");
                var function = Call ("Print", Expression.ConstantString ("from a function"));
                LinqExpression.Lambda<Action> (function).Compile () ();
            } finally {
                Logger.Writer = writer;
                Logger.Format = format;
            }
            CollectionAssert.AreEqual (
                new [] { "hello", "from a function" }, messages);
        }
    }
}
