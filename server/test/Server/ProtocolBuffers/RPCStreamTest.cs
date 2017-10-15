using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Google.Protobuf;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    public class RPCStreamTest
    {
        Schema.KRPC.Request expectedRequest;
        byte[] requestBytes;
        Response expectedResponseMessage;
        Schema.KRPC.Response expectedResponse;
        byte[] responseBytes;

        [SetUp]
        public void SetUp ()
        {
            // Create a request object and get the binary representation of it
            expectedRequest = new Schema.KRPC.Request ();
            expectedRequest.Calls.Add (
                new Schema.KRPC.ProcedureCall {
                    Service = "TestService",
                    Procedure = "ProcedureNoArgsNoReturn"
                });
            using (var stream = new MemoryStream ()) {
                var codedStream = new CodedOutputStream (stream, true);
                codedStream.WriteMessage (expectedRequest);
                codedStream.Flush();
                requestBytes = stream.ToArray ();
            }

            // Create a response object and get the binary representation of it
            expectedResponseMessage = new Response ();
            expectedResponseMessage.Error = new Error ("SomeErrorMessage", "StackTrace");
            expectedResponse = expectedResponseMessage.ToProtobufMessage ();
            using (var stream = new MemoryStream ()) {
                var codedStream = new CodedOutputStream (stream, true);
                codedStream.WriteMessage (expectedResponse);
                codedStream.Flush ();
                responseBytes = stream.ToArray ();
            }
        }

        [Test]
        public void Empty ()
        {
            var byteStream = new TestStream (new MemoryStream ());
            var rpcStream = new RPCStream (byteStream);
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        public void ReadSingleRequest ()
        {
            var byteStream = new TestStream (requestBytes);
            var rpcStream = new RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsTrue (rpcStream.DataAvailable);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (requestBytes.Length, rpcStream.BytesRead);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (requestBytes.Length, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        public void ReadMultipleRequests ()
        {
            var repeats = 5;
            var multipleRequestBytes = new byte [requestBytes.Length * repeats];
            for (int i = 0; i < repeats; i++)
                Array.Copy (requestBytes, 0, multipleRequestBytes, i * requestBytes.Length, requestBytes.Length);
            var byteStream = new TestStream (multipleRequestBytes);
            var rpcStream = new RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            for (int i = 0; i < repeats; i++) {
                Assert.IsTrue (rpcStream.DataAvailable);
                Assert.AreEqual (0, rpcStream.BytesWritten);
                Assert.AreEqual (multipleRequestBytes.Length, rpcStream.BytesRead);
                Request request = rpcStream.Read ();
                if (i < repeats - 1)
                    Assert.IsTrue (rpcStream.DataAvailable);
                else {
                    Assert.IsFalse (rpcStream.DataAvailable);
                    Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
                }
                Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
                Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
                Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
                Assert.AreEqual (0, rpcStream.BytesWritten);
                Assert.AreEqual (multipleRequestBytes.Length, rpcStream.BytesRead);
            }
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void ReadSingleRequestInParts ()
        {
            // Split the message bytes into 3 parts
            Assert.IsTrue (requestBytes.Length > 15);
            var part1 = new byte[4];
            var part2 = new byte[6];
            var part3 = new byte[requestBytes.Length - 10];
            Array.Copy (requestBytes, 0, part1, 0, part1.Length);
            Array.Copy (requestBytes, part1.Length, part2, 0, part2.Length);
            Array.Copy (requestBytes, part1.Length + part2.Length, part3, 0, part3.Length);

            // Write part 1
            var stream = new MemoryStream ();
            stream.Write (part1, 0, part1.Length);
            stream.Seek (0, SeekOrigin.Begin);

            // Read part 1
            var byteStream = new TestStream (stream);
            var rpcStream = new RPCStream (byteStream);
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length, rpcStream.BytesRead);

            // Write part 2
            Assert.AreEqual (part1.Length, stream.Position);
            stream.Write (part2, 0, part2.Length);
            stream.Seek (part1.Length, SeekOrigin.Begin);

            // Read part 2
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length + part2.Length, rpcStream.BytesRead);

            // Write part 3
            Assert.AreEqual (part1.Length + part2.Length, stream.Position);
            stream.Write (part3, 0, part3.Length);
            stream.Seek (-part3.Length, SeekOrigin.Current);

            // Read part 3
            Assert.IsTrue (rpcStream.DataAvailable);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length + part2.Length + part3.Length, rpcStream.BytesRead);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<KRPC.Server.Message.NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length + part2.Length + part3.Length, rpcStream.BytesRead);
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        public void ReadGarbage ()
        {
            var data = new byte [4000];
            var rand = new Random (42);
            rand.NextBytes (data);
            var byteStream = new TestStream (data);
            var rpcStream = new RPCStream (byteStream);
            Assert.Throws<KRPC.Server.Message.MalformedRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (data.Length, rpcStream.BytesRead);
            Assert.IsTrue (byteStream.Closed);
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var byteStream = new TestStream (null, stream);
            var rpcStream = new RPCStream (byteStream);
            rpcStream.Write (expectedResponseMessage);
            Assert.AreEqual (responseBytes.ToHexString (), stream.ToArray ().ToHexString ());
            Assert.AreEqual (responseBytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }
    }
}
