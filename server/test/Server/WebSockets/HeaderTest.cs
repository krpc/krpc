using KRPC.Server.Message;
using KRPC.Server.WebSockets;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    public class HeaderTest
    {
        static Header HeaderFromString (string data) {
            var buffer = data.ToBytes ();
            return Header.FromBytes (buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Decode a header from the given bytes (starting at index), check it decodes the expected length,
        /// and encode it again and compare it to canonicalData (expected shortest version of the header
        /// using the minimal number of payload length extension bytes).
        /// </summary>
        static void CheckBytes (string data, ulong expectedLength, string canonicalData, int index = 0)
        {
            var header = Header.FromBytes (data.ToBytes (), index, data.Length - index);
            Assert.IsTrue (header.FinalFragment);
            Assert.IsFalse (header.Rsv1);
            Assert.IsFalse (header.Rsv2);
            Assert.IsFalse (header.Rsv3);
            Assert.IsFalse (header.Masked);
            Assert.IsNull (header.MaskingKey);
            Assert.AreEqual (expectedLength, header.Length);
            Assert.AreEqual (canonicalData.Length / 2, header.HeaderLength);
            Assert.AreEqual (canonicalData, header.ToBytes ().ToHexString ());
        }

        /// <summary>
        /// Test headers with a payload size of 126 and less, where the payload size contains the length,
        /// and no payload length extension bytes are used.
        /// </summary>
        [Test]
        public void ShortPayloadLength ()
        {
            CheckBytes ("8200", 0, "8200");
            CheckBytes ("8201", 1, "8201");
            CheckBytes ("8202", 2, "8202");
            CheckBytes ("8210", 16, "8210");
            CheckBytes ("8212", 18, "8212");
            CheckBytes ("827c", 124, "827c");
            CheckBytes ("827d", 125, "827d");

            CheckBytes ("827e0000", 0, "8200");
            CheckBytes ("827e0001", 1, "8201");
            CheckBytes ("827e0002", 2, "8202");
            CheckBytes ("827e0010", 16, "8210");
            CheckBytes ("827e0012", 18, "8212");
            CheckBytes ("827e007c", 124, "827c");
            CheckBytes ("827e007d", 125, "827d");

            CheckBytes ("827f0000000000000000", 0, "8200");
            CheckBytes ("827f0000000000000001", 1, "8201");
            CheckBytes ("827f0000000000000002", 2, "8202");
            CheckBytes ("827f0000000000000010", 16, "8210");
            CheckBytes ("827f0000000000000012", 18, "8212");
            CheckBytes ("827f000000000000007c", 124, "827c");
            CheckBytes ("827f000000000000007d", 125, "827d");
        }

        /// <summary>
        /// Test headers with a payload size of 126, where 2 extension bytes contain the payload size.
        /// </summary>
        [Test]
        public void MediumPayloadLength ()
        {
            CheckBytes ("827e007e", 126, "827e007e");
            CheckBytes ("827e007f", 127, "827e007f");
            CheckBytes ("827e0080", 128, "827e0080");
            CheckBytes ("827e1234", 0x1234, "827e1234");
            CheckBytes ("827efffd", 0xfffd, "827efffd");
            CheckBytes ("827efffe", 0xfffe, "827efffe");
            CheckBytes ("827effff", 0xffff, "827effff");

            CheckBytes ("827f000000000000007e", 126, "827e007e");
            CheckBytes ("827f000000000000007f", 127, "827e007f");
            CheckBytes ("827f0000000000000080", 128, "827e0080");
            CheckBytes ("827f0000000000001234", 0x1234, "827e1234");
            CheckBytes ("827f000000000000fffd", 0xfffd, "827efffd");
            CheckBytes ("827f000000000000fffe", 0xfffe, "827efffe");
            CheckBytes ("827f000000000000ffff", 0xffff, "827effff");
        }

        /// <summary>
        /// Test headers with a payload size of 127, where 8 extension bytes contain the payload size.
        /// </summary>
        [Test]
        public void LongPayloadLength ()
        {
            CheckBytes ("827f0000000000010000", 0x10000, "827f0000000000010000");
            CheckBytes ("827f0000000000010001", 0x10001, "827f0000000000010001");
            CheckBytes ("827f0000000000010002", 0x10002, "827f0000000000010002");
            CheckBytes ("827f1234567890123456", 0x1234567890123456, "827f1234567890123456");
            CheckBytes ("827fffffffffffffffff", 0xffffffffffffffff, "827fffffffffffffffff");
        }

        [Test]
        public void FromBytesWithOffset ()
        {
            CheckBytes ("ffff8212ffff", 18, "8212", 2);
            CheckBytes ("ffff827e0012ffff", 18, "8212", 2);
            CheckBytes ("ffff827f0000000000000012ffff", 18, "8212", 2);
            CheckBytes ("ffff827e1234ffff", 0x1234, "827e1234", 2);
            CheckBytes ("ffff827f0000000000001234ffff", 0x1234, "827e1234", 2);
            CheckBytes ("ffff827f1234567890123456ffff", 0x1234567890123456, "827f1234567890123456", 2);
        }

        [Test]
        public void FromBytesTruncated ()
        {
            Assert.Throws<NoRequestException> (() => HeaderFromString ("82"));
            Assert.Throws<NoRequestException> (() => HeaderFromString ("827f"));
            Assert.Throws<NoRequestException> (() => HeaderFromString ("827f12"));
            Assert.Throws<NoRequestException> (() => HeaderFromString ("827f1234"));
        }

        [Test]
        public void FromBytesWithTrailingData ()
        {
            CheckBytes ("8212ffff", 18, "8212");
            CheckBytes ("827e0012ffff", 18, "8212");
            CheckBytes ("827f0000000000000012ffff", 18, "8212");
            CheckBytes ("827e1234ffff", 0x1234, "827e1234");
            CheckBytes ("827f0000000000001234ffff", 0x1234, "827e1234");
            CheckBytes ("827f1234567890123456ffff", 0x1234567890123456, "827f1234567890123456");
        }

        [Test]
        public void OpCodeToBytes ()
        {
            Assert.AreEqual ("8800", new Header (OpCode.Close, 0).ToBytes ().ToHexString ());
            Assert.AreEqual ("8900", new Header (OpCode.Ping, 0).ToBytes ().ToHexString ());
            Assert.AreEqual ("8a00", new Header (OpCode.Pong, 0).ToBytes ().ToHexString ());
            Assert.AreEqual ("8200", new Header (OpCode.Binary, 0).ToBytes ().ToHexString ());
            Assert.AreEqual ("8100", new Header (OpCode.Text, 0).ToBytes ().ToHexString ());
            Assert.AreEqual ("8000", new Header (OpCode.Continue, 0).ToBytes ().ToHexString ());
        }

        [Test]
        public void FinalFragment ()
        {
            var request1 = HeaderFromString ("a212");
            Assert.IsTrue (request1.FinalFragment);
            Assert.IsFalse (request1.Rsv1);
            Assert.IsTrue (request1.Rsv2);
            Assert.IsFalse (request1.Rsv3);
            Assert.IsFalse (request1.Masked);
            Assert.AreEqual (18, request1.Length);
            Assert.AreEqual (2, request1.HeaderLength);

            var request2 = HeaderFromString ("2212");
            Assert.IsFalse (request2.FinalFragment);
            Assert.IsFalse (request2.Rsv1);
            Assert.IsTrue (request2.Rsv2);
            Assert.IsFalse (request2.Rsv3);
            Assert.IsFalse (request2.Masked);
            Assert.AreEqual (18, request1.Length);
            Assert.AreEqual (2, request1.HeaderLength);
        }

        [Test]
        public void WithReservations ()
        {
            var request1 = HeaderFromString ("f212");
            Assert.IsTrue (request1.FinalFragment);
            Assert.IsTrue (request1.Rsv1);
            Assert.IsTrue (request1.Rsv2);
            Assert.IsTrue (request1.Rsv3);
            Assert.IsFalse (request1.Masked);
            Assert.AreEqual (18, request1.Length);
            Assert.AreEqual (2, request1.HeaderLength);

            var request2 = HeaderFromString ("a212");
            Assert.IsTrue (request2.FinalFragment);
            Assert.IsFalse (request2.Rsv1);
            Assert.IsTrue (request2.Rsv2);
            Assert.IsFalse (request2.Rsv3);
            Assert.IsFalse (request2.Masked);
            Assert.AreEqual (18, request1.Length);
            Assert.AreEqual (2, request1.HeaderLength);
        }

        [Test]
        public void WithMaskingKey ()
        {
            var request = HeaderFromString ("828012345678");
            Assert.IsTrue (request.Masked);
            Assert.AreEqual (new byte[] { 0x12, 0x34, 0x56, 0x78 }, request.MaskingKey);
            Assert.IsTrue (request.FinalFragment);
            Assert.IsFalse (request.Rsv1);
            Assert.IsFalse (request.Rsv2);
            Assert.IsFalse (request.Rsv3);
            Assert.AreEqual (0, request.Length);
            Assert.AreEqual (6, request.HeaderLength);
        }

        [Test]
        public void MissingMaskingKey ()
        {
            Assert.Throws<NoRequestException> (() => HeaderFromString ("8280"));
        }
    }
}
