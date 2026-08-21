using System;
using NUnit.Framework;
using StdLib = KRPC.Service.StdLib.StdLib;
using Tuple3 = System.Tuple<double, double, double>;
using Tuple4 = System.Tuple<double, double, double, double>;

namespace KRPC.Test.Service.StdLib
{
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
            Assert.AreEqual (Math.PI, global::KRPC.Service.StdLib.StdLib.Pi);
            Assert.AreEqual (Math.E, global::KRPC.Service.StdLib.StdLib.E);
            Assert.AreEqual (4.2, global::KRPC.Service.StdLib.StdLib.Abs (-4.2));
            Assert.AreEqual (-1, global::KRPC.Service.StdLib.StdLib.Sign (-0.1));
            Assert.AreEqual (-4, global::KRPC.Service.StdLib.StdLib.Floor (-3.5));
            Assert.AreEqual (4, global::KRPC.Service.StdLib.StdLib.Ceiling (3.5));
            Assert.AreEqual (4, global::KRPC.Service.StdLib.StdLib.Round (3.5));
            Assert.AreEqual (3, global::KRPC.Service.StdLib.StdLib.Sqrt (9));
            Assert.AreEqual (1, global::KRPC.Service.StdLib.StdLib.Sin (Math.PI / 2), 1e-10);
            Assert.AreEqual (-1, global::KRPC.Service.StdLib.StdLib.Cos (Math.PI), 1e-10);
            Assert.AreEqual (1, global::KRPC.Service.StdLib.StdLib.Tan (Math.PI / 4), 1e-10);
            Assert.AreEqual (Math.PI / 2, global::KRPC.Service.StdLib.StdLib.Asin (1), 1e-10);
            Assert.AreEqual (0, global::KRPC.Service.StdLib.StdLib.Acos (1), 1e-10);
            Assert.AreEqual (Math.PI / 4, global::KRPC.Service.StdLib.StdLib.Atan (1), 1e-10);
            Assert.AreEqual (Math.PI / 2, global::KRPC.Service.StdLib.StdLib.Atan2 (1, 0), 1e-10);
            Assert.AreEqual (1, global::KRPC.Service.StdLib.StdLib.Log (Math.E), 1e-10);
            Assert.AreEqual (2, global::KRPC.Service.StdLib.StdLib.Log10 (100), 1e-10);
            Assert.AreEqual (Math.E, global::KRPC.Service.StdLib.StdLib.Exp (1), 1e-10);
            Assert.AreEqual (1, global::KRPC.Service.StdLib.StdLib.Min (1, 2));
            Assert.AreEqual (2, global::KRPC.Service.StdLib.StdLib.Max (1, 2));
            Assert.AreEqual (5, global::KRPC.Service.StdLib.StdLib.Clamp (7, 0, 5));
            Assert.AreEqual (0, global::KRPC.Service.StdLib.StdLib.Clamp (-7, 0, 5));
            Assert.AreEqual (Math.PI, global::KRPC.Service.StdLib.StdLib.DegreesToRadians (180), 1e-10);
            Assert.AreEqual (180, global::KRPC.Service.StdLib.StdLib.RadiansToDegrees (Math.PI), 1e-10);
        }

        [Test]
        public void Vectors ()
        {
            var x = new Tuple3 (1, 0, 0);
            var y = new Tuple3 (0, 1, 0);
            var z = new Tuple3 (0, 0, 1);
            AssertVectorsEqual (new Tuple3 (1, 1, 0), global::KRPC.Service.StdLib.StdLib.VectorAdd (x, y));
            AssertVectorsEqual (new Tuple3 (1, -1, 0), global::KRPC.Service.StdLib.StdLib.VectorSubtract (x, y));
            AssertVectorsEqual (new Tuple3 (2.5, 0, 0), global::KRPC.Service.StdLib.StdLib.VectorScale (x, 2.5));
            Assert.AreEqual (0, global::KRPC.Service.StdLib.StdLib.VectorDot (x, y));
            AssertVectorsEqual (z, global::KRPC.Service.StdLib.StdLib.VectorCross (x, y));
            Assert.AreEqual (5, global::KRPC.Service.StdLib.StdLib.VectorMagnitude (new Tuple3 (3, 4, 0)));
            AssertVectorsEqual (
                new Tuple3 (0.6, 0.8, 0),
                global::KRPC.Service.StdLib.StdLib.VectorNormalize (new Tuple3 (3, 4, 0)));
            Assert.AreEqual (5, global::KRPC.Service.StdLib.StdLib.VectorDistance (new Tuple3 (1, 1, 0), new Tuple3 (4, 5, 0)));
            Assert.AreEqual (Math.PI / 2, global::KRPC.Service.StdLib.StdLib.VectorAngle (x, y), 1e-10);
            AssertVectorsEqual (
                new Tuple3 (0.5, 0.5, 0),
                global::KRPC.Service.StdLib.StdLib.VectorLerp (x, y, 0.5));
            Assert.Throws<ArgumentException> (
                () => global::KRPC.Service.StdLib.StdLib.VectorNormalize (new Tuple3 (0, 0, 0)));
        }

        [Test]
        public void Quaternions ()
        {
            var identity = new Tuple4 (0, 0, 0, 1);
            var zAxis = new Tuple3 (0, 0, 1);
            // 90 degrees about the z axis
            var quarter = global::KRPC.Service.StdLib.StdLib.QuaternionFromAxisAngle (zAxis, Math.PI / 2);
            AssertQuaternionsEqual (
                new Tuple4 (0, 0, Math.Sin (Math.PI / 4), Math.Cos (Math.PI / 4)), quarter);

            // Rotating the x axis 90 degrees about z gives the y axis
            AssertVectorsEqual (
                new Tuple3 (0, 1, 0),
                global::KRPC.Service.StdLib.StdLib.QuaternionRotateVector (quarter, new Tuple3 (1, 0, 0)));

            // Composing two quarter turns gives a half turn
            var half = global::KRPC.Service.StdLib.StdLib.QuaternionMultiply (quarter, quarter);
            AssertVectorsEqual (
                new Tuple3 (-1, 0, 0),
                global::KRPC.Service.StdLib.StdLib.QuaternionRotateVector (half, new Tuple3 (1, 0, 0)));

            // A rotation composed with its inverse is the identity
            AssertQuaternionsEqual (
                identity,
                global::KRPC.Service.StdLib.StdLib.QuaternionMultiply (
                    quarter, global::KRPC.Service.StdLib.StdLib.QuaternionInverse (quarter)));

            Assert.AreEqual (
                Math.PI / 2,
                global::KRPC.Service.StdLib.StdLib.QuaternionAngle (identity, quarter), 1e-10);

            // Interpolating half way between the identity and a half turn
            // gives a quarter turn
            AssertQuaternionsEqual (
                quarter,
                global::KRPC.Service.StdLib.StdLib.QuaternionSlerp (identity, half, 0.5));
        }
    }
}
