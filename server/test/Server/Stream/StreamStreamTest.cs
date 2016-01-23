using NUnit.Framework;
using System.IO;
using System.Linq;
using KRPC.Server.Stream;
using KRPC.Schema.KRPC;
using Google.Protobuf;

namespace KRPC.Test.Server.Stream
{
    [TestFixture]
    public class StreamStreamTest
    {
        StreamMessage expectedMessage;
        byte[] messageBytes;

        [SetUp]
        public void SetUp ()
        {
            // Create a response object and get the binary representation of it
            var streamMessage = new StreamMessage ();

            var response1 = new Response ();
            response1.Time = 42;
            response1.Error = "Foo";

            var streamResponse1 = new StreamResponse ();
            streamResponse1.Id = 1263;

            var response2 = new Response ();
            response2.Time = 123;
            response2.Error = "Bar";

            var streamResponse2 = new StreamResponse ();
            streamResponse2.Id = 3443;
            streamResponse2.Response = response2;

            streamMessage.Responses.Add (streamResponse1);
            streamMessage.Responses.Add (streamResponse2);

            expectedMessage = streamMessage;
            using (var stream = new MemoryStream ()) {
                expectedMessage.WriteDelimitedTo (stream);
                messageBytes = stream.ToArray ();
            }
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var streamStream = new StreamStream (new TestStream (null, stream));
            Assert.AreEqual (0, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            streamStream.Write (expectedMessage);
            byte[] bytes = stream.ToArray ();
            Assert.AreEqual (bytes.Length, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            Assert.IsTrue (messageBytes.SequenceEqual (bytes));
        }
    }
}
