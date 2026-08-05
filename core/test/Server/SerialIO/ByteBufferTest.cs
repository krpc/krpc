using System;
using KRPC.Server.SerialIO;
using NUnit.Framework;

namespace KRPC.Test.Server.SerialIO
{
    [TestFixture]
    public class ByteBufferTest
    {
        static byte[] Sequence (int start, int length)
        {
            var data = new byte [length];
            for (int i = 0; i < length; i++)
                data [i] = (byte)((start + i) % 256);
            return data;
        }

        [Test]
        public void Empty ()
        {
            var buffer = new ByteBuffer ();
            Assert.AreEqual (0, buffer.Length);
            Assert.AreEqual (0, buffer.Take (new byte [16], 0, 16));
        }

        [Test]
        public void AppendAndTake ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2, 3, 4 }, 0, 4);
            Assert.AreEqual (4, buffer.Length);
            var result = new byte [4];
            Assert.AreEqual (4, buffer.Take (result, 0, 4));
            Assert.AreEqual (new byte[] { 1, 2, 3, 4 }, result);
            Assert.AreEqual (0, buffer.Length);
        }

        [Test]
        public void AppendPartOfABuffer ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2, 3, 4, 5 }, 1, 3);
            var result = new byte [3];
            Assert.AreEqual (3, buffer.Take (result, 0, 3));
            Assert.AreEqual (new byte[] { 2, 3, 4 }, result);
        }

        [Test]
        public void TakeIntoPartOfABuffer ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2 }, 0, 2);
            var result = new byte [4];
            Assert.AreEqual (2, buffer.Take (result, 1, 3));
            Assert.AreEqual (new byte[] { 0, 1, 2, 0 }, result);
        }

        [Test]
        public void TakeMoreThanIsHeld ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2, 3 }, 0, 3);
            var result = new byte [8];
            Assert.AreEqual (3, buffer.Take (result, 0, 8));
            Assert.AreEqual (new byte[] { 1, 2, 3, 0, 0, 0, 0, 0 }, result);
            Assert.AreEqual (0, buffer.Length);
        }

        [Test]
        public void TakeLessThanIsHeld ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            var result = new byte [2];
            Assert.AreEqual (2, buffer.Take (result, 0, 2));
            Assert.AreEqual (new byte[] { 1, 2 }, result);
            Assert.AreEqual (3, buffer.Length);
            result = new byte [3];
            Assert.AreEqual (3, buffer.Take (result, 0, 3));
            Assert.AreEqual (new byte[] { 3, 4, 5 }, result);
            Assert.AreEqual (0, buffer.Length);
        }

        [Test]
        public void OrderIsPreservedAcrossAppends ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2 }, 0, 2);
            buffer.Append (new byte[] { 3 }, 0, 1);
            buffer.Append (new byte[] { 4, 5, 6 }, 0, 3);
            var result = new byte [6];
            Assert.AreEqual (6, buffer.Take (result, 0, 6));
            Assert.AreEqual (new byte[] { 1, 2, 3, 4, 5, 6 }, result);
        }

        [Test]
        public void Clear ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (new byte[] { 1, 2, 3 }, 0, 3);
            buffer.Clear ();
            Assert.AreEqual (0, buffer.Length);
            buffer.Append (new byte[] { 4 }, 0, 1);
            var result = new byte [1];
            Assert.AreEqual (1, buffer.Take (result, 0, 1));
            Assert.AreEqual (new byte[] { 4 }, result);
        }

        /// <summary>
        /// Data larger than the buffer starts out at must be held in full.
        /// </summary>
        [Test]
        public void GrowsForDataLargerThanItself ()
        {
            var buffer = new ByteBuffer ();
            var data = Sequence (0, 100 * 1024);
            buffer.Append (data, 0, data.Length);
            Assert.AreEqual (data.Length, buffer.Length);
            var result = new byte [data.Length];
            Assert.AreEqual (data.Length, buffer.Take (result, 0, result.Length));
            Assert.AreEqual (data, result);
        }

        /// <summary>
        /// Filling and draining repeatedly moves the data through the backing array many times
        /// over, which must compact rather than grow it or run off the end.
        /// </summary>
        [Test]
        public void RepeatedFillAndDrain ()
        {
            var buffer = new ByteBuffer ();
            var chunk = new byte [1000];
            var result = new byte [1000];
            for (int i = 0; i < 1000; i++) {
                Array.Copy (Sequence (i, chunk.Length), chunk, chunk.Length);
                buffer.Append (chunk, 0, chunk.Length);
                Assert.AreEqual (chunk.Length, buffer.Take (result, 0, result.Length));
                Assert.AreEqual (chunk, result);
                Assert.AreEqual (0, buffer.Length);
            }
        }

        /// <summary>
        /// Data appended while earlier data is still held must not disturb it, whether the append
        /// fits behind it or forces the buffer to be compacted or grown.
        /// </summary>
        [Test]
        public void AppendWhileDataIsStillHeld ()
        {
            var buffer = new ByteBuffer ();
            var chunk = Sequence (0, 3 * 1024);
            for (int i = 0; i < 10; i++)
                buffer.Append (chunk, 0, chunk.Length);
            Assert.AreEqual (10 * chunk.Length, buffer.Length);
            var result = new byte [chunk.Length];
            for (int i = 0; i < 10; i++) {
                Assert.AreEqual (chunk.Length, buffer.Take (result, 0, result.Length));
                Assert.AreEqual (chunk, result);
            }
            Assert.AreEqual (0, buffer.Length);
        }

        /// <summary>
        /// Taking part of the held data leaves the rest at an offset into the backing array, which
        /// a following append has to account for.
        /// </summary>
        [Test]
        public void AppendAfterAPartialTake ()
        {
            var buffer = new ByteBuffer ();
            buffer.Append (Sequence (0, 6 * 1024), 0, 6 * 1024);
            var taken = new byte [5 * 1024];
            Assert.AreEqual (taken.Length, buffer.Take (taken, 0, taken.Length));
            Assert.AreEqual (Sequence (0, taken.Length), taken);
            buffer.Append (Sequence (100, 6 * 1024), 0, 6 * 1024);
            Assert.AreEqual (1024 + 6 * 1024, buffer.Length);
            var result = new byte [1024];
            Assert.AreEqual (result.Length, buffer.Take (result, 0, result.Length));
            Assert.AreEqual (Sequence (5 * 1024, 1024), result);
            result = new byte [6 * 1024];
            Assert.AreEqual (result.Length, buffer.Take (result, 0, result.Length));
            Assert.AreEqual (Sequence (100, 6 * 1024), result);
        }
    }
}
