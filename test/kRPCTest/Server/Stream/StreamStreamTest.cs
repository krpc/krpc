using NUnit.Framework;
using System.IO;
using System.Linq;
using KRPC.Server.Stream;
using KRPC.Schema.KRPC;

namespace KRPCTest.Server.Stream
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
            var streamMessageBuilder = StreamMessage.CreateBuilder ();

            var responseBuilder1 = Response.CreateBuilder ();
            responseBuilder1.SetTime (42);
            responseBuilder1.SetError ("Foo");

            var streamResponseBuilder1 = StreamResponse.CreateBuilder ();
            streamResponseBuilder1.SetId (1263);
            streamResponseBuilder1.SetResponse (responseBuilder1.Build ());

            var responseBuilder2 = Response.CreateBuilder ();
            responseBuilder2.SetTime (123);
            responseBuilder2.SetError ("Bar");

            var streamResponseBuilder2 = StreamResponse.CreateBuilder ();
            streamResponseBuilder2.SetId (3443);
            streamResponseBuilder2.SetResponse (responseBuilder2.Build ());

            streamMessageBuilder.AddResponses (streamResponseBuilder1.Build ());
            streamMessageBuilder.AddResponses (streamResponseBuilder2.Build ());

            expectedMessage = streamMessageBuilder.Build ();
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
            streamStream.Write (expectedMessage);
            byte[] bytes = stream.ToArray ();
            Assert.IsTrue (messageBytes.SequenceEqual (bytes));
        }
    }
}
