using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Google.Protobuf;
using KRPC.Server.Message;
using KRPC.Server.ProtocolBuffers;
using KRPC.Server.WebSockets;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
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
                expectedRequest.WriteTo (stream);
                stream.Flush ();
                requestBytes = stream.ToArray ();
            }

            // Create a response object and get the binary representation of it
            expectedResponseMessage = new Response ();
            expectedResponseMessage.Error = new Error ("SomeErrorMessage", "StackTrace");
            expectedResponse = expectedResponseMessage.ToProtobufMessage ();
            using (var stream = new MemoryStream ()) {
                expectedResponse.WriteTo (stream);
                stream.Flush ();
                responseBytes = stream.ToArray ();
            }
        }

        [Test]
        public void Empty ()
        {
            var byteStream = new TestStream (new MemoryStream ());
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        public void ReadBinaryFrameWithUnmaskedPayload ()
        {
            var stream = new MemoryStream (new Frame (OpCode.Binary, requestBytes).ToBytes ());
            var errorStream = new MemoryStream ();
            var errorBytes = Frame.Close (1002, "Payload is not masked").ToBytes ();
            var byteStream = new TestStream (stream, errorStream);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.Throws<MalformedRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (errorBytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (stream.Length, rpcStream.BytesRead);
            Assert.IsTrue (byteStream.Closed);
            Assert.AreEqual (errorBytes.ToHexString (), errorStream.ToArray ().ToHexString ());
        }

        [Test]
        public void ReadSingleRequest ()
        {
            var frameBytes = Frame.Binary (requestBytes, "deadbeef".ToBytes ()).ToBytes ();
            var byteStream = new TestStream (frameBytes);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsTrue (rpcStream.DataAvailable);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (frameBytes.Length, rpcStream.BytesRead);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (frameBytes.Length, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        public void ReadMultipleRequests ()
        {
            var repeats = 5;
            var frameBytes = Frame.Binary (requestBytes, "deadbeef".ToBytes ()).ToBytes ();
            var multipleFrameBytes = new byte [frameBytes.Length * repeats];
            for (int i = 0; i < repeats; i++)
                Array.Copy (frameBytes, 0, multipleFrameBytes, i * frameBytes.Length, frameBytes.Length);
            var byteStream = new TestStream (multipleFrameBytes);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            for (int i = 0; i < repeats; i++) {
                Assert.IsTrue (rpcStream.DataAvailable);
                Assert.AreEqual (0, rpcStream.BytesWritten);
                Assert.AreEqual (multipleFrameBytes.Length, rpcStream.BytesRead);
                Request request = rpcStream.Read ();
                if (i < repeats - 1)
                    Assert.IsTrue (rpcStream.DataAvailable);
                else {
                    Assert.IsFalse (rpcStream.DataAvailable);
                    Assert.Throws<NoRequestException> (() => rpcStream.Read ());
                }
                Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
                Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
                Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
                Assert.AreEqual (0, rpcStream.BytesWritten);
                Assert.AreEqual (multipleFrameBytes.Length, rpcStream.BytesRead);
            }
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void ReadSingleRequestInParts ()
        {
            var frameBytes = Frame.Binary (requestBytes, "deadbeef".ToBytes ()).ToBytes ();

            // Split the message bytes into 3 parts
            Assert.IsTrue (frameBytes.Length > 15);
            var part1 = new byte[4];
            var part2 = new byte[6];
            var part3 = new byte[frameBytes.Length - 10];
            Array.Copy (frameBytes, 0, part1, 0, part1.Length);
            Array.Copy (frameBytes, part1.Length, part2, 0, part2.Length);
            Array.Copy (frameBytes, part1.Length + part2.Length, part3, 0, part3.Length);

            // Write part 1
            var stream = new MemoryStream ();
            stream.Write (part1, 0, part1.Length);
            stream.Seek (0, SeekOrigin.Begin);

            // Read part 1
            var byteStream = new TestStream (stream);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length, rpcStream.BytesRead);

            // Write part 2
            Assert.AreEqual (part1.Length, stream.Position);
            stream.Write (part2, 0, part2.Length);
            stream.Seek (part1.Length, SeekOrigin.Begin);

            // Read part 2
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
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
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (part1.Length + part2.Length + part3.Length, rpcStream.BytesRead);
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void ReadSingleRequestInMultipleFrames ()
        {
            // Split the message bytes into 3 parts
            Assert.IsTrue (requestBytes.Length > 15);
            var part1 = new byte[4];
            var part2 = new byte[6];
            var part3 = new byte[requestBytes.Length - 10];
            Array.Copy (requestBytes, 0, part1, 0, part1.Length);
            Array.Copy (requestBytes, part1.Length, part2, 0, part2.Length);
            Array.Copy (requestBytes, part1.Length + part2.Length, part3, 0, part3.Length);

            var maskingKey = "deadbeef".ToBytes ();
            var frame1 = new Frame (OpCode.Binary, part1);
            frame1.Header.FinalFragment = false;
            frame1.Header.MaskingKey = maskingKey;
            var frame2 = new Frame (OpCode.Continue, part2);
            frame2.Header.FinalFragment = false;
            frame2.Header.MaskingKey = maskingKey;
            var frame3 = new Frame (OpCode.Continue, part3);
            frame3.Header.MaskingKey = maskingKey;

            var frame1Bytes = frame1.ToBytes ();
            var frame2Bytes = frame2.ToBytes ();
            var frame3Bytes = frame3.ToBytes ();

            var frames = new MemoryStream (frame1.Length + frame2.Length + frame3.Length);
            frames.Write (frame1Bytes, 0, frame1Bytes.Length);
            frames.Write (frame2Bytes, 0, frame2Bytes.Length);
            frames.Write (frame3Bytes, 0, frame3Bytes.Length);
            frames.Seek (0, SeekOrigin.Begin);

            var byteStream = new TestStream (frames);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsTrue (rpcStream.DataAvailable);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (frame1.Length + frame2.Length + frame3.Length, rpcStream.BytesRead);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (frame1.Length + frame2.Length + frame3.Length, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void ReadSingleRequestInMultipleFramesInterleavedWithPings ()
        {
            var pingFrame = new Frame (OpCode.Ping, "deadbeef".ToBytes ());
            pingFrame.Header.MaskingKey = "12345678".ToBytes ();
            var pingFrameBytes = pingFrame.ToBytes ();
            var expectedPongFrameBytes = Frame.Pong (pingFrame.Payload).ToBytes ();

            // Split the message bytes into 3 parts
            Assert.IsTrue (requestBytes.Length > 15);
            var part1 = new byte[4];
            var part2 = new byte[6];
            var part3 = new byte[requestBytes.Length - 10];
            Array.Copy (requestBytes, 0, part1, 0, part1.Length);
            Array.Copy (requestBytes, part1.Length, part2, 0, part2.Length);
            Array.Copy (requestBytes, part1.Length + part2.Length, part3, 0, part3.Length);

            var maskingKey = "deadbeef".ToBytes ();
            var frame1 = new Frame (OpCode.Binary, part1);
            frame1.Header.FinalFragment = false;
            frame1.Header.MaskingKey = maskingKey;
            var frame2 = new Frame (OpCode.Continue, part2);
            frame2.Header.FinalFragment = false;
            frame2.Header.MaskingKey = maskingKey;
            var frame3 = new Frame (OpCode.Continue, part3);
            frame3.Header.MaskingKey = maskingKey;

            var frame1Bytes = frame1.ToBytes ();
            var frame2Bytes = frame2.ToBytes ();
            var frame3Bytes = frame3.ToBytes ();

            var totalSize = frame1Bytes.Length + frame2Bytes.Length + frame3Bytes.Length + 7 * pingFrameBytes.Length;
            var frames = new MemoryStream (totalSize);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (frame1Bytes, 0, frame1Bytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (frame2Bytes, 0, frame2Bytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Write (frame3Bytes, 0, frame3Bytes.Length);
            frames.Write (pingFrameBytes, 0, pingFrameBytes.Length);
            frames.Seek (0, SeekOrigin.Begin);

            var pongStream = new MemoryStream ();
            var byteStream = new TestStream (frames, pongStream);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.AreEqual (0, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsTrue (rpcStream.DataAvailable);
            Assert.AreEqual (6 * expectedPongFrameBytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (totalSize, rpcStream.BytesRead);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.Throws<NoRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (expectedRequest.Calls.Count, request.Calls.Count);
            Assert.AreEqual (expectedRequest.Calls [0].Service, request.Calls [0].Service);
            Assert.AreEqual (expectedRequest.Calls [0].Procedure, request.Calls [0].Procedure);
            Assert.AreEqual (7 * expectedPongFrameBytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (totalSize, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
            Assert.AreEqual (
                string.Concat (Enumerable.Repeat (expectedPongFrameBytes.ToHexString (), 7)),
                pongStream.ToArray ().ToHexString ());
        }

        [Test]
        public void ReadGarbage ()
        {
            var data = new byte [4000];
            var rand = new Random (42);
            rand.NextBytes (data);
            var errorStream = new MemoryStream ();
            var errorBytes = Frame.Close (1002, "Invalid op code").ToBytes ();
            var byteStream = new TestStream (new MemoryStream (data), errorStream);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            Assert.Throws<MalformedRequestException> (() => rpcStream.Read ());
            Assert.AreEqual (errorBytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (data.Length, rpcStream.BytesRead);
            Assert.IsTrue (byteStream.Closed);
            Assert.AreEqual (errorBytes.ToHexString (), errorStream.ToArray ().ToHexString ());
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var byteStream = new TestStream (null, stream);
            var rpcStream = new KRPC.Server.WebSockets.RPCStream (byteStream);
            rpcStream.Write (expectedResponseMessage);
            byte[] bytes = stream.ToArray ();
            var frameBytes = Frame.Binary (responseBytes).ToBytes ();
            Assert.AreEqual (frameBytes.ToHexString (), bytes.ToHexString ());
            Assert.AreEqual (bytes.Length, rpcStream.BytesWritten);
            Assert.AreEqual (0, rpcStream.BytesRead);
            Assert.IsFalse (byteStream.Closed);
        }
    }
}
