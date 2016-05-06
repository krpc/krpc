using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.WebSockets;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
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
            streamResponse1.Response = response1;

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
                var codedStream = new CodedOutputStream (stream);
                expectedMessage.ToProtobufMessage ().WriteTo (codedStream);
                codedStream.Flush ();
                messageBytes = Frame.Binary (stream.ToArray ()).ToBytes ();
            }
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var streamStream = new KRPC.Server.WebSockets.StreamStream (new TestStream (null, stream));
            Assert.AreEqual (0, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            streamStream.Write (expectedMessage);
            Assert.AreEqual (messageBytes.Length, streamStream.BytesWritten);
            Assert.AreEqual (0, streamStream.BytesRead);
            Assert.AreEqual (messageBytes.ToHexString (), stream.ToArray ().ToHexString ());
        }
    }
}
