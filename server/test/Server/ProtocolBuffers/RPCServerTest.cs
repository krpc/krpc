using System;
using System.IO;
using System.Linq;
using System.Text;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class RPCServerTest
    {
        byte[] helloMessage;

        [SetUp]
        public void SetUp ()
        {
            helloMessage = new byte[12 + 32];
            byte[] header = { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x2D, 0x52, 0x50, 0x43, 0x00, 0x00, 0x00 };
            Array.Copy (header, helloMessage, header.Length);
            const string name = "Jebediah Kerman!!!";
            var encoder = new UTF8Encoding (false, true);
            byte[] nameBytes = encoder.GetBytes (name);
            Array.Copy (nameBytes, 0, helloMessage, header.Length, nameBytes.Length);
        }

        [Test]
        public void ValidHelloMessageWithNoName ()
        {
            for (int i = 12; i < helloMessage.Length; i++)
                helloMessage [i] = 0x00;

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (helloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var byteClient = new TestClient (stream);

            var server = new RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsTrue (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (1, server.Clients.Count ());
            Assert.AreEqual (String.Empty, server.Clients.First ().Name);

            byte[] bytes = responseStream.ToArray ();
            byte[] responseBytes = byteClient.Guid.ToByteArray ();
            Assert.IsTrue (responseBytes.SequenceEqual (bytes));
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

            var server = new RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsTrue (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (1, server.Clients.Count ());
            Assert.AreEqual ("Jebediah Kerman!!!", server.Clients.First ().Name);

            byte[] bytes = responseStream.ToArray ();
            byte[] responseBytes = byteClient.Guid.ToByteArray ();
            Assert.IsTrue (responseBytes.SequenceEqual (bytes));
        }

        [Test]
        public void InvalidHelloMessageHeader ()
        {
            helloMessage [4] = 0x42;

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (helloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new RPCServer (byteServer);
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
        public void InvalidHelloMessageName ()
        {
            helloMessage [15] = 0x00;

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (helloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new RPCServer (byteServer);
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
            var responseStream = new MemoryStream ();

            var message = new byte[] { 0x48, 0x45, 0x4C };
            var stream = new TestStream (new MemoryStream (message), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new RPCServer (byteServer);
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
        public void ShortHelloMessageName ()
        {
            var shortHelloMessage = new byte[8 + 31];
            Array.Copy (helloMessage, shortHelloMessage, shortHelloMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (shortHelloMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new RPCServer (byteServer);
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

            var server = new RPCServer (byteServer);
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
