using KRPC.Server.Message;
using KRPC.Server.WebSockets;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    public class FrameTest
    {
        static Frame FrameFromString (string data) {
            var buffer = data.ToBytes ();
            return Frame.FromBytes (buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Decode a frame from data, check it decodes the expected header and unmasked payload,
        /// and encode it again and compare it to the original data.
        /// </summary>
        static void CheckBytes (string data, string expectedHeader, string expectedPayload)
        {
            var frame = FrameFromString (data);
            Assert.AreEqual (expectedHeader, frame.Header.ToBytes ().ToHexString ());
            if (expectedPayload.Length == 0)
                Assert.IsEmpty (frame.Payload);
            else
                Assert.AreEqual (expectedPayload, frame.Payload.ToHexString ());
            Assert.AreEqual (data.Length / 2, frame.Length);
            Assert.IsFalse (frame.IsPartial);
            Assert.AreEqual (data, frame.ToBytes ().ToHexString ());
        }

        [Test]
        public void FromBytesEmptyUnmaskedPayload ()
        {
            CheckBytes ("8800", "8800", string.Empty);
        }

        [Test]
        public void FromBytesEmptyMaskedPayload ()
        {
            CheckBytes ("888000000000", "888000000000", string.Empty);
        }

        [Test]
        public void FromBytesMaskedPayload ()
        {
            CheckBytes ("88810000000012", "888100000000", "12");
            CheckBytes ("888412345678cc99e897", "888412345678", "deadbeef");
            CheckBytes ("889312345678cc99e89795511559cc99e89795511559cc99e8",
                "889312345678", "deadbeef87654321deadbeef87654321deadbe");
        }

        [Test]
        public void FromBytesTruncatedHeader ()
        {
            Assert.Throws<NoRequestException> (() => FrameFromString ("88"));
        }

        [Test]
        public void FromBytesTruncatedPayloadEmpty ()
        {
            var frame = FrameFromString ("888412345678");
            Assert.AreEqual ("888412345678", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual (string.Empty, frame.Payload.ToHexString ());
            Assert.AreEqual (6, frame.Length);
            Assert.IsTrue (frame.IsPartial);
        }

        [Test]
        public void FromBytesTruncatedPayloadPartial ()
        {
            var frame = FrameFromString ("888412345678cc99");
            Assert.AreEqual ("888412345678", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("dead", frame.Payload.ToHexString ());
            Assert.AreEqual (8, frame.Length);
            Assert.IsTrue (frame.IsPartial);
        }

        [Test]
        public void FromBytesExtra ()
        {
            var frame = FrameFromString ("888412345678cc99e8971234");
            Assert.AreEqual ("888412345678", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("deadbeef", frame.Payload.ToHexString ());
            Assert.AreEqual (10, frame.Length);
            Assert.IsFalse (frame.IsPartial);
            Assert.AreEqual ("888412345678cc99e897", frame.ToBytes ().ToHexString ());
        }

        [Test]
        public void WithOpCodeOnly ()
        {
            var frame = new Frame (OpCode.Binary);
            Assert.AreEqual ("8200", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8200", frame.Header.ToBytes ().ToHexString ());
            Assert.IsEmpty (frame.Payload);
            Assert.AreEqual (2, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void WithPayload ()
        {
            var payload = new byte [] { 0xde, 0xad, 0xbe, 0xef };
            var frame = new Frame (OpCode.Binary, payload);
            Assert.AreEqual ("8204deadbeef", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8204", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("deadbeef", frame.Payload.ToHexString ());
            Assert.AreEqual (6, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void CloseFrameWithoutPayload ()
        {
            var frame = Frame.Close ();
            Assert.AreEqual ("8800", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8800", frame.Header.ToBytes ().ToHexString ());
            Assert.IsEmpty (frame.Payload);
            Assert.AreEqual (2, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void CloseFrameWithStatus ()
        {
            var frame = Frame.Close (4660);
            Assert.AreEqual ("88021234", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8802", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("1234", frame.Payload.ToHexString ());
            Assert.AreEqual (4, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void CloseFrameWithStatusAndMessage ()
        {
            var frame = Frame.Close (4660, "jeb");
            Assert.AreEqual ("880512346a6562", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8805", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("12346a6562", frame.Payload.ToHexString ());
            Assert.AreEqual (7, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void CloseFrameWithStatusFromPayload ()
        {
            var frame = Frame.Close ("1234".ToBytes ());
            Assert.AreEqual ("88021234", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8802", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("1234", frame.Payload.ToHexString ());
            Assert.AreEqual (4, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void CloseFrameWithStatusFromPayloadWithExtraBytes ()
        {
            var frame = Frame.Close ("1234deadbeef".ToBytes ());
            Assert.AreEqual ("88021234", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8802", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("1234", frame.Payload.ToHexString ());
            Assert.AreEqual (4, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }

        [Test]
        public void PongFrame ()
        {
            var frame = Frame.Pong ("1234567890".ToBytes ());
            Assert.AreEqual ("8a051234567890", frame.ToBytes ().ToHexString ());
            Assert.AreEqual ("8a05", frame.Header.ToBytes ().ToHexString ());
            Assert.AreEqual ("1234567890", frame.Payload.ToHexString ());
            Assert.AreEqual (7, frame.Length);
            Assert.IsFalse (frame.IsPartial);
        }
    }
}
