using Google.Protobuf;
using KRPC.Schema.KRPC;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class SchemaTest
    {
        [Test]
        public void SimpleProtobufUsage ()
        {
            const string SERVICE = "a";
            const string METHOD = "b";

            var call = new ProcedureCall ();
            call.Service = SERVICE;
            call.Procedure = METHOD;

            Assert.AreEqual (METHOD, call.Procedure);
            Assert.AreEqual (SERVICE, call.Service);

            var buffer = new byte [call.CalculateSize ()];
            var stream = new CodedOutputStream (buffer);
            call.WriteTo (stream);

            var reqCopy = ProcedureCall.Parser.ParseFrom (buffer);

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
            Assert.AreEqual ("ac02", buffer.ToHexString ());
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
