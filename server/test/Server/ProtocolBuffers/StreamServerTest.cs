using System;
using System.IO;
using System.Linq;
using KRPC.Schema.KRPC;
using KRPC.Server;
using KRPC.Server.ProtocolBuffers;
using Moq;
using NUnit.Framework;

using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;

namespace KRPC.Test.Server.ProtocolBuffers
{
    [TestFixture]
    public class StreamServerTest
    {
        Guid clientId = new Guid ("1234567890abcdef1234567890abcdef".ToBytes ());

        [Test]
        public void ValidConnectionMessage ()
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (TestingTools.CreateConnectionRequest (clientId.ToByteArray ())), responseStream);

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
            Assert.AreEqual (clientId, server.Clients.First ().Guid);

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 19, Status.Ok, String.Empty, 16);
        }

        [Test]
        public void InvalidConnectionMessageHeader ()
        {
            var responseStream = new MemoryStream ();

            var connectionMessage = TestingTools.CreateConnectionRequest (clientId.ToByteArray ());
            connectionMessage [2] ^= 0x42;
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 209, Status.MalformedMessage,
                "While parsing a protocol message, the input ended unexpectedly in the middle of a field.  " +
                "This could mean either than the input has been truncated or that an embedded message misreported its own length.", 0);
        }

        [Test]
        public void ShortConnectionMessageHeader ()
        {
            var connectionMessage = new byte[5];
            Array.Copy (TestingTools.CreateConnectionRequest (clientId.ToByteArray ()), connectionMessage, connectionMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 33, Status.Timeout, "The operation has timed out.", 0);
        }

        [Test]
        public void InvalidConnectionMessageIdentifier ()
        {
            var connectionMessage = TestingTools.CreateConnectionRequest ("123456".ToBytes ());

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 40, Status.MalformedMessage, "Client identifier must be 16 bytes.", 0);
        }

        [Test]
        public void NoConnectionMessage ()
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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 33, Status.Timeout, "The operation has timed out.", 0);
        }
    }
}
