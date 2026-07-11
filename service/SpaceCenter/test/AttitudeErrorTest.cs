using System;
using NUnit.Framework;

namespace KRPC.SpaceCenter.AutoPilot
{
    [TestFixture]
    public class AttitudeErrorTest
    {
        struct QuaternionComponents
        {
            public readonly double X;
            public readonly double Y;
            public readonly double Z;
            public readonly double W;

            public QuaternionComponents (double x, double y, double z, double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }
        }

        [Test]
        public void IdentityReturnsExactZero ()
        {
            AssertVector (Vector (new QuaternionComponents (0, 0, 0, 1)), 0, 0, 0, 0);
            AssertVector (Vector (new QuaternionComponents (0, 0, 0, -1)), 0, 0, 0, 0);
        }

        [Test]
        public void EquivalentQuaternionSignsReturnSameVector ()
        {
            var axes = new [] {
                new [] { 1.0, 0.0, 0.0 },
                new [] { 0.0, 1.0, 0.0 },
                new [] { 0.0, 0.0, 1.0 },
                new [] { 1.0, -2.0, 3.0 }
            };
            var angles = new [] { 0.0, 1.0, 90.0, 170.0, 179.0, 180.0, 181.0, 190.0, 270.0, 359.0 };
            foreach (var axis in axes) {
                foreach (var angle in angles) {
                    var quaternion = AngleAxis (angle, axis [0], axis [1], axis [2]);
                    AssertEqual (Vector (quaternion), Vector (Negate (quaternion)), 1e-10);
                }
            }
        }

        [Test]
        public void SmallSingleAxisErrorsKeepAxisAndSign ()
        {
            AssertVector (Vector (AngleAxis (5, 1, 0, 0)), 5, 0, 0);
            AssertVector (Vector (AngleAxis (-5, 1, 0, 0)), -5, 0, 0);
            AssertVector (Vector (AngleAxis (5, 0, 1, 0)), 0, 5, 0);
            AssertVector (Vector (AngleAxis (-5, 0, 1, 0)), 0, -5, 0);
            AssertVector (Vector (AngleAxis (5, 0, 0, 1)), 0, 0, 5);
            AssertVector (Vector (AngleAxis (-5, 0, 0, 1)), 0, 0, -5);
        }

        [Test]
        public void LargeAnglesUseShortestRotation ()
        {
            var cases = new [] {
                new [] { 90.0, 90.0 },
                new [] { 170.0, 170.0 },
                new [] { 179.0, 179.0 },
                new [] { 180.0, 180.0 },
                new [] { 181.0, -179.0 },
                new [] { 190.0, -170.0 },
                new [] { 270.0, -90.0 }
            };
            foreach (var item in cases)
                AssertVector (Vector (AngleAxis (item [0], 1, 0, 0)), item [1], 0, 0);
        }

        [Test]
        public void ExactHalfTurnUsesDeterministicAxis ()
        {
            var positive = new QuaternionComponents (1, 0, 0, 0);
            var negative = new QuaternionComponents (-1, 0, 0, 0);
            AssertEqual (Vector (positive), Vector (negative), 0);
            AssertVector (Vector (negative), 180, 0, 0);
            AssertVector (Vector (new QuaternionComponents (0, -1, 0, 0)), 0, 180, 0);
            AssertVector (Vector (new QuaternionComponents (0, 0, -1, 0)), 0, 0, 180);

            var mixed = new QuaternionComponents (1, -2, 3, 0);
            var componentScale = 180.0 / Math.Sqrt (14.0);
            AssertVector (
                Vector (mixed), componentScale, -2 * componentScale, 3 * componentScale);
            AssertEqual (Vector (mixed), Vector (Negate (mixed)), 0);
        }

        [Test]
        public void ExactHalfTurnTieBreaksLargestAxisInXyzOrder ()
        {
            var twoWayScale = 180.0 / Math.Sqrt (2.0);
            AssertVector (
                Vector (new QuaternionComponents (-1, -1, 0, 0)),
                twoWayScale, twoWayScale, 0);
            AssertVector (
                Vector (new QuaternionComponents (-1, 1, 0, 0)),
                twoWayScale, -twoWayScale, 0);
            AssertEqual (
                Vector (new QuaternionComponents (1, 1, 0, 0)),
                Vector (new QuaternionComponents (-1, -1, 0, 0)), 0);
            AssertEqual (
                Vector (new QuaternionComponents (1, -1, 0, 0)),
                Vector (new QuaternionComponents (-1, 1, 0, 0)), 0);

            var threeWayScale = 180.0 / Math.Sqrt (3.0);
            AssertVector (
                Vector (new QuaternionComponents (1, 1, 1, 0)),
                threeWayScale, threeWayScale, threeWayScale);
            AssertVector (
                Vector (new QuaternionComponents (-1, -1, -1, 0)),
                threeWayScale, threeWayScale, threeWayScale);
        }

        [Test]
        public void ExactHalfTurnTieBreakHandlesNearlyEqualLargestAxes ()
        {
            var tinyDelta = 4e-16;
            var vector = Vector (new QuaternionComponents (-1, 1 + tinyDelta, 0, 0));
            AssertVector (
                vector, -180.0 / Math.Sqrt (2.0), 180.0 / Math.Sqrt (2.0), 0, 1e-13);
            AssertEqual (
                vector,
                Vector (new QuaternionComponents (1, -(1 + tinyDelta), 0, 0)), 1e-13);
        }

        [Test]
        public void ExactHalfTurnTieBreakIgnoresMinorAxisNoise ()
        {
            var positiveNoise = Vector (new QuaternionComponents (1e-16, 1, 0, 0));
            var negativeNoise = Vector (new QuaternionComponents (-1e-16, 1, 0, 0));
            AssertVector (positiveNoise, 0, 180, 0, 1e-10);
            AssertVector (negativeNoise, 0, 180, 0, 1e-10);
        }

        [Test]
        public void PiNeighborhoodIsFiniteShortestAndSignInvariant ()
        {
            var epsilons = new [] { 1e-6, 1e-10, 1e-14 };
            foreach (var epsilon in epsilons) {
                var below = AngleAxis (180.0 - epsilon, 1, -1, 1);
                var at = new QuaternionComponents (1, -1, 1, 0);
                var above = AngleAxis (180.0 + epsilon, 1, -1, 1);

                AssertFinite (Vector (below));
                AssertFinite (Vector (at));
                AssertFinite (Vector (above));
                AssertMagnitude (Vector (below), 180.0 - epsilon, 1e-10);
                AssertMagnitude (Vector (at), 180.0, 1e-10);
                AssertMagnitude (Vector (above), 180.0 - epsilon, 1e-10);
                AssertEqual (Vector (below), Vector (Negate (below)), 1e-10);
                AssertEqual (Vector (at), Vector (Negate (at)), 0);
                AssertEqual (Vector (above), Vector (Negate (above)), 1e-10);
            }
        }

        [Test]
        public void HeadingWrapUsesTwoDegreePath ()
        {
            var current = AngleAxis (359, 0, 0, 1);
            var target = AngleAxis (1, 0, 0, 1);
            AssertVector (Vector (Multiply (target, Inverse (current))), 0, 0, 2);

            current = AngleAxis (1, 0, 0, 1);
            target = AngleAxis (359, 0, 0, 1);
            AssertVector (Vector (Multiply (target, Inverse (current))), 0, 0, -2);
        }

        [Test]
        public void RollWrapUsesTwoDegreePath ()
        {
            var current = AngleAxis (179, 0, 1, 0);
            var target = AngleAxis (-179, 0, 1, 0);
            AssertVector (Vector (Multiply (target, Inverse (current))), 0, 2, 0);

            current = AngleAxis (-179, 0, 1, 0);
            target = AngleAxis (179, 0, 1, 0);
            AssertVector (Vector (Multiply (target, Inverse (current))), 0, -2, 0);
        }

        [Test]
        public void CombinedRotationIsFiniteAndSignInvariant ()
        {
            var rotation = Multiply (AngleAxis (90, 0, 0, 1), AngleAxis (90, 1, 0, 0));
            var vector = Vector (rotation);
            var component = 120.0 / Math.Sqrt (3.0);
            AssertVector (vector, component, component, component);
            AssertFinite (vector);
            AssertEqual (vector, Vector (Negate (rotation)), 1e-10);
        }

        [Test]
        public void NonUnitAndNearZeroMagnitudeQuaternionsAreNormalizedSafely ()
        {
            var quaternion = AngleAxis (73, 1, -2, 3);
            var componentScale = 73.0 / Math.Sqrt (14.0);
            var expected = new RotationVector (componentScale, -2 * componentScale, 3 * componentScale);
            AssertEqual (expected, Vector (quaternion), 1e-10);
            AssertEqual (expected, Vector (Scale (quaternion, 7)), 1e-10);
            AssertEqual (expected, Vector (Scale (quaternion, 1e-300)), 1e-10);
            AssertEqual (expected, Vector (Scale (quaternion, 1e300)), 1e-10);
        }

        [Test]
        public void VerySmallRotationDoesNotUnderflowToZero ()
        {
            var expected = 2e-200 * (180.0 / Math.PI);
            AssertVector (
                Vector (new QuaternionComponents (1e-200, 0, 0, 1)), expected, 0, 0, 1e-210);
        }

        [Test]
        public void InvalidQuaternionIsRejectedWithoutNonFiniteOutput ()
        {
            var zero = Assert.Throws<ArgumentException> (
                () => Vector (new QuaternionComponents (0, 0, 0, 0)));
            Assert.AreEqual ("Quaternion must have non-zero magnitude", zero.Message);

            var nan = Assert.Throws<ArgumentException> (
                () => Vector (new QuaternionComponents (double.NaN, 0, 0, 1)));
            Assert.AreEqual ("Quaternion components must be finite", nan.Message);

            var positiveInfinity = Assert.Throws<ArgumentException> (
                () => Vector (new QuaternionComponents (0, double.PositiveInfinity, 0, 1)));
            Assert.AreEqual ("Quaternion components must be finite", positiveInfinity.Message);

            var negativeInfinity = Assert.Throws<ArgumentException> (
                () => Vector (new QuaternionComponents (0, 0, double.NegativeInfinity, 1)));
            Assert.AreEqual ("Quaternion components must be finite", negativeInfinity.Message);
        }

        static RotationVector Vector (QuaternionComponents quaternion)
        {
            return AttitudeError.ToRotationVector (
                quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        static QuaternionComponents AngleAxis (double angle, double x, double y, double z)
        {
            var axisMagnitude = Math.Sqrt (x * x + y * y + z * z);
            x /= axisMagnitude;
            y /= axisMagnitude;
            z /= axisMagnitude;
            var halfAngle = angle * (Math.PI / 180.0) * 0.5;
            var sin = Math.Sin (halfAngle);
            return new QuaternionComponents (x * sin, y * sin, z * sin, Math.Cos (halfAngle));
        }

        static QuaternionComponents Multiply (QuaternionComponents lhs, QuaternionComponents rhs)
        {
            return new QuaternionComponents (
                lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y,
                lhs.W * rhs.Y - lhs.X * rhs.Z + lhs.Y * rhs.W + lhs.Z * rhs.X,
                lhs.W * rhs.Z + lhs.X * rhs.Y - lhs.Y * rhs.X + lhs.Z * rhs.W,
                lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z);
        }

        static QuaternionComponents Inverse (QuaternionComponents quaternion)
        {
            return new QuaternionComponents (
                -quaternion.X, -quaternion.Y, -quaternion.Z, quaternion.W);
        }

        static QuaternionComponents Negate (QuaternionComponents quaternion)
        {
            return Scale (quaternion, -1);
        }

        static QuaternionComponents Scale (QuaternionComponents quaternion, double scale)
        {
            return new QuaternionComponents (
                quaternion.X * scale, quaternion.Y * scale,
                quaternion.Z * scale, quaternion.W * scale);
        }

        static void AssertFinite (RotationVector vector)
        {
            Assert.IsFalse (double.IsNaN (vector.X) || double.IsInfinity (vector.X));
            Assert.IsFalse (double.IsNaN (vector.Y) || double.IsInfinity (vector.Y));
            Assert.IsFalse (double.IsNaN (vector.Z) || double.IsInfinity (vector.Z));
        }

        static void AssertEqual (RotationVector lhs, RotationVector rhs, double tolerance)
        {
            Assert.AreEqual (lhs.X, rhs.X, tolerance);
            Assert.AreEqual (lhs.Y, rhs.Y, tolerance);
            Assert.AreEqual (lhs.Z, rhs.Z, tolerance);
        }

        static void AssertMagnitude (RotationVector vector, double expected, double tolerance)
        {
            var actual = Math.Sqrt (vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            Assert.AreEqual (expected, actual, tolerance);
        }

        static void AssertVector (
            RotationVector actual, double x, double y, double z, double tolerance = 1e-10)
        {
            Assert.AreEqual (x, actual.X, tolerance);
            Assert.AreEqual (y, actual.Y, tolerance);
            Assert.AreEqual (z, actual.Z, tolerance);
            AssertFinite (actual);
        }
    }
}
