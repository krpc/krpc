using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class StreamStreamTest
    {
        StreamUpdate expectedUpdate;
        byte[] updateBytes;

        [SetUp]
        public void SetUp ()
        {
            // Create a response object and get the binary representation of it
            var streamUpdate = new StreamUpdate ();

            var response1 = new Response ();
            response1.Time = 42;
            response1.Error = "Foo";

            var streamResult1 = new StreamResult (1263);
            streamResult1.Response = response1;

            var response2 = new Response ();
            response2.Time = 123;
            response2.Error = "Bar";

            var streamResult2 = new StreamResult (3443);
            streamResult2.Response = response2;

            streamUpdate.Results.Add (streamResult1);
            streamUpdate.Results.Add (streamResult2);

            expectedUpdate = streamUpdate;
            using (var stream = new MemoryStream ()) {
                expectedUpdate.ToProtobufMessage ().WriteDelimitedTo (stream);
                updateBytes = stream.ToArray ();
            }
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var streamStream = new StreamStream (new TestStream (null, stream));
            Assert.AreEqual (0, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            streamStream.Write (expectedUpdate);
            Assert.AreEqual (updateBytes.Length, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            Assert.AreEqual (updateBytes.ToHexString (), stream.ToArray ().ToHexString ());
        }
    }
}
