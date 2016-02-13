using System;
using System.IO;
using Google.Protobuf;
using NUnit.Framework;
using KRPC.Schema.KRPC;

namespace KRPC.Test.Schema
{
    [TestFixture]
    public class RpcTest
    {
        [Test]
        public void SimpleProtobufUsage ()
        {
            const string SERVICE = "a";
            const string METHOD = "b";

            var request = new Request ();
            request.Service = SERVICE;
            request.Procedure = METHOD;

            Assert.AreEqual (METHOD, request.Procedure);
            Assert.AreEqual (SERVICE, request.Service);

            var buffer = new byte [request.CalculateSize ()];
            var stream = new CodedOutputStream (buffer);
            request.WriteTo (stream);

            Request reqCopy = Request.Parser.ParseFrom (buffer);

            Assert.AreEqual (METHOD, reqCopy.Procedure);
            Assert.AreEqual (SERVICE, reqCopy.Service);
        }

        [Test]
        public void ValueTypeToByteString ()
        {
            // From Google's protobuf documentation, varint encoding example:
            // 300 = 1010 1100 0000 0010 = 0xAC 0x02
            const int value = 300;
            var buffer = new byte [2];
            var codedStream = new CodedOutputStream (buffer);
            codedStream.WriteUInt32 (value);
            string hex = ("0x" + BitConverter.ToString (buffer)).Replace ("-", " 0x");
            Assert.AreEqual ("0xAC 0x02", hex);
        }

        [Test]
        public void ByteStringToValueType ()
        {
            // From Google's protobuf documentation, varint encoding example:
            // 300 = 1010 1100 0000 0010 = 0xAC 0x02
            byte[] bytes = { 0xAC, 0x02 };
            var codedStream = new CodedInputStream (bytes);
            uint value = codedStream.ReadUInt32 ();
            Assert.AreEqual (300, value);
        }
    }
}

