using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using KRPC.Server.RPC;
using KRPC.Schema.KRPC;

namespace KRPCTest.Server.RPC
{
    [TestFixture]
    public class RPCStreamTest
    {
        Request expectedRequest;
        byte[] requestBytes;
        Response expectedResponse;
        byte[] responseBytes;

        [SetUp]
        public void SetUp ()
        {
            // Create a request object and get the binary representation of it
            var requestBuilder = Request.CreateBuilder ();
            requestBuilder.Service = "SomeServiceName";
            requestBuilder.Procedure = "SomeMethodName";
            expectedRequest = requestBuilder.Build ();
            using (var stream = new MemoryStream ()) {
                expectedRequest.WriteDelimitedTo (stream);
                requestBytes = stream.ToArray ();
            }

            // Create a response object and get the binary representation of it
            var responseBuilder = Response.CreateBuilder ();
            responseBuilder.Error = "SomeErrorMessage";
            responseBuilder.Time = 42;
            expectedResponse = responseBuilder.Build ();
            using (var stream = new MemoryStream ()) {
                expectedResponse.WriteDelimitedTo (stream);
                responseBytes = stream.ToArray ();
            }
        }

        [Test]
        public void Empty ()
        {
            var stream = new TestStream (new MemoryStream ());
            var rpcStream = new RPCStream (stream);
            Assert.IsFalse (rpcStream.DataAvailable);
            //rpcStream.Read ();
        }

        [Test]
        public void ReadSingleRequest ()
        {
            var stream = new TestStream (new MemoryStream (requestBytes));
            var rpcStream = new RPCStream (stream);
            Assert.IsTrue (rpcStream.DataAvailable);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.AreEqual (expectedRequest.Service, request.Service);
            Assert.AreEqual (expectedRequest.Procedure, request.Procedure);
        }

        [Test]
        public void ReadSingleRequestInParts ()
        {
            // Split the message bytes into 3 parts
            Assert.IsTrue (requestBytes.Length > 15);
            byte[] part1 = new byte[4];
            byte[] part2 = new byte[6];
            byte[] part3 = new byte[requestBytes.Length - 10];
            Array.Copy (requestBytes, 0, part1, 0, part1.Length);
            Array.Copy (requestBytes, part1.Length, part2, 0, part2.Length);
            Array.Copy (requestBytes, part1.Length + part2.Length, part3, 0, part3.Length);

            // Write part 1
            var stream = new MemoryStream ();
            stream.Write (part1, 0, part1.Length);
            stream.Seek (0, SeekOrigin.Begin);

            // Read part 1
            var rpcStream = new RPCStream (new TestStream (stream));
            Assert.IsFalse (rpcStream.DataAvailable);

            // Write part 2
            Assert.AreEqual (part1.Length, stream.Position);
            stream.Write (part2, 0, part2.Length);
            stream.Seek (part1.Length, SeekOrigin.Begin);

            // Read part 2
            Assert.IsFalse (rpcStream.DataAvailable);

            // Write part 3
            Assert.AreEqual (part1.Length + part2.Length, stream.Position);
            stream.Write (part3, 0, part3.Length);
            stream.Seek (-part3.Length, SeekOrigin.Current);

            // Read part 3
            Assert.IsTrue (rpcStream.DataAvailable);
            Request request = rpcStream.Read ();
            Assert.IsFalse (rpcStream.DataAvailable);
            Assert.AreEqual (expectedRequest.Service, request.Service);
            Assert.AreEqual (expectedRequest.Procedure, request.Procedure);
        }

        [Test]
        public void ReadMalformedRequest ()
        {
            // A request without all of the required fields filled in
            var builder = Request.CreateBuilder ();
            builder.Service = "SomeServiceName";
            expectedRequest = builder.BuildPartial ();
            var stream = new MemoryStream ();
            expectedRequest.WriteDelimitedTo (stream);
            stream.Seek (0, SeekOrigin.Begin);
            var rpcStream = new RPCStream (new TestStream (stream));
            Assert.Throws<MalformedRequestException> (() => rpcStream.Read ());
        }

        [Test]
        public void ReadGarbage ()
        {
            byte[] data = new byte[RPCStream.bufferSize + 1];
            Random rand = new Random (42);
            rand.NextBytes (data);
            var rpcStream = new RPCStream (new TestStream (new MemoryStream (data)));
            Assert.Throws<RequestBufferOverflowException> (() => rpcStream.Read ());
        }

        [Test]
        public void WriteSingleResponse ()
        {
            var stream = new MemoryStream ();
            var rpcStream = new RPCStream (new TestStream (stream));
            rpcStream.Write (expectedResponse);
            byte[] bytes = stream.ToArray ();
            Assert.IsTrue (responseBytes.SequenceEqual (bytes));
        }
    }
}
