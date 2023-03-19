using System;
using KRPC.Utils;
using NUnit.Framework;

namespace KRPC.Test.Utils
{
    [TestFixture]
    public class DynamicBufferTest
    {
        [Test]
        public void Empty ()
        {
            var buffer = new DynamicBuffer ();
            Assert.AreEqual (0, buffer.Length);
            CollectionAssert.AreEqual (new byte[] { }, buffer.ToArray ());
        }

        [Test]
        public void Append ()
        {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var buffer = new DynamicBuffer ();
            buffer.Append (data, 0, data.Length);
            Assert.AreEqual (data.Length, buffer.Length);
            CollectionAssert.AreEqual (data, buffer.ToArray ());
            Assert.AreEqual (32 * 1024, buffer.GetBuffer ().Length);
        }

        [Test]
        public void AppendMany ()
        {
            const int numRepeats = 100;
            var data = new byte[16 * 1024 + 3];
            for (int i = 0; i < data.Length; i++)
                data [i] = (byte)i;

            var buffer = new DynamicBuffer ();
            for (int i = 0; i < numRepeats; i++) {
                buffer.Append (data, 0, data.Length);
                Assert.AreEqual (data.Length * (i + 1), buffer.Length);
            }
            var expectedData = new byte[data.Length * numRepeats];
            for (int i = 0; i < numRepeats; i++)
                Array.Copy (data, 0, expectedData, data.Length * i, data.Length);
            CollectionAssert.AreEqual (expectedData, buffer.ToArray ());
            Assert.AreEqual (51 * 32 * 1024, buffer.GetBuffer ().Length);
        }

        [Test]
        public void SetLength ()
        {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var buffer = new DynamicBuffer ();
            buffer.Append (data, 0, data.Length);
            Assert.AreEqual (data.Length, buffer.Length);
            CollectionAssert.AreEqual (data, buffer.ToArray ());
            buffer.Length = 2;
            CollectionAssert.AreEqual (new byte[] { 0x01, 0x02 }, buffer.ToArray ());
            buffer.Length = 0;
            CollectionAssert.AreEqual (new byte[] { }, buffer.ToArray ());
        }
    }
}
