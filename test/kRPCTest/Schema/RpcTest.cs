using System;
using System.IO;
using Google.ProtocolBuffers;
using NUnit.Framework;
using KRPC.Schema.KRPC;

namespace KRPCTest.Schema
{
    [TestFixture]
    public class RpcTest
    {
        [Test]
        public void SimpleProtobufUsage ()
        {
            const string SERVICE = "a";
            const string METHOD = "b";

            var builder = Request.CreateBuilder ();
            builder.Service = SERVICE;
            builder.Procedure = METHOD;
            var request = builder.Build ();

            Assert.IsTrue (request.HasProcedure);
            Assert.IsTrue (request.HasService);
            Assert.AreEqual (METHOD, request.Procedure);
            Assert.AreEqual (SERVICE, request.Service);

            var stream = new MemoryStream ();
            request.WriteDelimitedTo (stream);

            stream.Seek (0, SeekOrigin.Begin);

            Request reqCopy = Request.ParseDelimitedFrom (stream);

            Assert.IsTrue (reqCopy.HasProcedure);
            Assert.IsTrue (reqCopy.HasService);
            Assert.AreEqual (METHOD, reqCopy.Procedure);
            Assert.AreEqual (SERVICE, reqCopy.Service);
        }

        [Test]
        public void ValueTypeToByteString ()
        {
            // From Google's protobuf documentation, varint encoding example:
            // 300 = 1010 1100 0000 0010 = 0xAC 0x02
            const int value = 300;
            var stream = new MemoryStream ();
            var codedStream = CodedOutputStream.CreateInstance (stream);
            codedStream.WriteUInt32NoTag (value);
            codedStream.Flush ();
            string hex = ("0x" + BitConverter.ToString (stream.ToArray ())).Replace ("-", " 0x");
            Assert.AreEqual ("0xAC 0x02", hex);
        }

        [Test]
        public void ByteStringToValueType ()
        {
            // From Google's protobuf documentation, varint encoding example:
            // 300 = 1010 1100 0000 0010 = 0xAC 0x02
            byte[] bytes = { 0xAC, 0x02 };
            var codedStream = CodedInputStream.CreateInstance (bytes);
            uint value = 0;
            codedStream.ReadUInt32 (ref value);
            Assert.AreEqual (300, value);
        }
    }
}

