using System;
using System.IO;
using System.Linq;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class StreamServerTest
    {
        byte[] helloMessage;
        Guid clientGuid;

        [SetUp]
        public void SetUp ()
        {
            helloMessage = new byte[12 + 16];
            byte[] header = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x53, 0x54, 0x52, 0x45, 0x41, 0x4D };
            Array.Copy (header, helloMessage, header.Length);
            clientGuid = new Guid ("1234567890abcdef1234567890abcdef".ToBytes ());
            byte[] identifier = clientGuid.ToByteArray ();
            Array.Copy (identifier, 0, helloMessage, header.Length, identifier.Length);
        }

        [Test]
        public void ValidHelloMessage ()
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (helloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var byteClient = new TestClient (stream);

            var server = new StreamServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsTrue (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (1, server.Clients.Count ());
            Assert.AreEqual (clientGuid, server.Clients.First ().Guid);

            byte[] bytes = responseStream.ToArray ();
            byte[] expectedBytes = { 0x4F, 0x4B };
            Assert.IsTrue (expectedBytes.SequenceEqual (bytes));
        }

        [Test]
        public void InvalidHelloMessageHeader ()
        {
            var responseStream = new MemoryStream ();

            helloMessage [4] = 0x42;
            var stream = new TestStream (new MemoryStream (helloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new StreamServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (0, responseStream.Length);
        }

        [Test]
        public void ShortHelloMessageHeader ()
        {
            var shortHelloMessage = new byte[5];
            Array.Copy (helloMessage, shortHelloMessage, shortHelloMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (shortHelloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new StreamServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (0, responseStream.Length);
        }

        [Test]
        public void ShortHelloMessageIdentifier ()
        {
            var shortHelloMessage = new byte[8 + 15];
            Array.Copy (helloMessage, shortHelloMessage, shortHelloMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (shortHelloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new StreamServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (0, responseStream.Length);
        }

        [Test]
        public void NoHelloMessage ()
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new StreamServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (0, responseStream.Length);
        }
    }
}
