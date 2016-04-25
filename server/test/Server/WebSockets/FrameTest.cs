using System;
using System.Linq;
using KRPC.Server.WebSockets;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    public class FrameTest
    {
        static string ToHex (byte[] data)
        {
            return BitConverter.ToString (data).Replace ("-", "").ToLower ();
        }

        static byte[] FromHex (string data)
        {
            return Enumerable.Range (0, data.Length)
                    .Where (x => x % 2 == 0)
                    .Select (x => Convert.ToByte (data.Substring (x, 2), 16))
                    .ToArray ();
        }

        [Test]
        public void WithOpCodeOnly ()
        {
            var frame = new Frame (OpCode.Binary);
            Assert.AreEqual ("8200", ToHex (frame.ToBytes ()));
            Assert.AreEqual ("8200", ToHex (frame.Header.ToBytes ()));
            Assert.IsNull (frame.Payload);
            Assert.AreEqual (2, frame.Length);
        }

        [Test]
        public void WithPayload ()
        {
            var payload = new byte [] { 0xde, 0xad, 0xbe, 0xef };
            var frame = new Frame (OpCode.Binary, payload);
            Assert.AreEqual ("8204deadbeef", ToHex (frame.ToBytes ()));
            Assert.AreEqual ("8204", ToHex (frame.Header.ToBytes ()));
            Assert.AreEqual ("deadbeef", ToHex (frame.Payload));
            Assert.AreEqual (6, frame.Length);
        }

        [Test]
        public void CloseFrame ()
        {
            //FIXME: doesn't include message
            var frame = Frame.Close (1234, "message");
            Assert.AreEqual ("8802d204", ToHex (frame.ToBytes ()));
            Assert.AreEqual ("8802", ToHex (frame.Header.ToBytes ()));
            Assert.AreEqual ("d204", ToHex (frame.Payload));
        }

        /// <summary>
        /// Decode a frame from the given bytes, check it decodes the expected header and unmasked payload,
        /// and encode it again and compare it to the expected bytes.
        /// </summary>
        static void CheckBytes (string data, string header, string payload)
        {
            var frame = Frame.FromBytes (FromHex (data));
            Assert.AreEqual (header, ToHex (frame.Header.ToBytes ()));
            if (payload.Length == 0)
                Assert.IsNull (frame.Payload);
            else
                Assert.AreEqual (payload, ToHex (frame.Payload));
            Assert.AreEqual (data.Length / 2, frame.Length);
            Assert.AreEqual (data, ToHex (frame.ToBytes ()));
        }

        [Test]
        public void FromBytesEmptyUnmaskedPayload ()
        {
            CheckBytes ("8800", "8800", "");
        }

        [Test]
        public void FromBytesEmptyMaskedPayload ()
        {
            CheckBytes ("888000000000", "888000000000", "");
        }

        [Test]
        public void FromBytesMaskedPayload ()
        {
            CheckBytes ("88810000000012", "888100000000", "12");
            CheckBytes ("888412345678cc99e897", "888412345678", "deadbeef");
            CheckBytes ("889312345678cc99e89795511559cc99e89795511559cc99e8", "889312345678", "deadbeef87654321deadbeef87654321deadbe");
        }
    }
}
