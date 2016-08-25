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
    public class RPCServerTest
    {
        [Test]
        public void ValidConnectionMessage ()
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (TestingTools.CreateRPCConnectionRequest ()), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 19, Status.Ok, String.Empty, 16);
        }

        [Test]
        public void InvalidConnectionMessageHeader ()
        {
            var connectionMessage = TestingTools.CreateRPCConnectionRequest ();
            connectionMessage [4] = 0x42;

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 3, Status.MalformedHeader, String.Empty, 0);
        }

        [Test]
        public void ShortConnectionMessageHeader ()
        {
            var connectionMessage = new byte[5];
            Array.Copy (TestingTools.CreateRPCConnectionRequest (), connectionMessage, connectionMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 33, Status.Timeout, "The operation has timed out.", 0);
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

            var server = new RPCServer (byteServer);
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
