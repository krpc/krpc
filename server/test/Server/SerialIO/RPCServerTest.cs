using System;
using System.IO;
using System.Linq;
using KRPC.Server;
using KRPC.Server.SerialIO;
using KRPC.Server.ProtocolBuffers;
using Moq;
using NUnit.Framework;
using Status = KRPC.Schema.KRPC.ConnectionResponse.Types.Status;
using Type = KRPC.Schema.KRPC.ConnectionRequest.Types.Type;

namespace KRPC.Test.Server.SerialIO
{
    [TestFixture]
    public class RPCServerTest
    {
        [Test]
        public void ValidConnectionMessage ()
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (TestingTools.CreateConnectionRequest (Type.Rpc)), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var byteClient = new TestClient (stream);

            var server = new KRPC.Server.SerialIO.RPCServer (byteServer);
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

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 19, Status.Ok, string.Empty, 16);
        }

        [Test]
        public void WrongConnectionType ()
        {
            var connectionMessage = TestingTools.CreateConnectionRequest (Type.Stream);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new KRPC.Server.SerialIO.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 75, Status.WrongType,
                "Connection request was for a stream server, but this is an rpc server.", 0);
        }

        [Test]
        public void InvalidConnectionMessageHeader ()
        {
            var connectionMessage = TestingTools.CreateConnectionRequest (Type.Rpc);
            connectionMessage [2] ^= 0x42;
            connectionMessage [3] ^= 0x42;
            connectionMessage [4] ^= 0x42;
            connectionMessage [5] ^= 0x42;

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new KRPC.Server.SerialIO.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            TestingTools.CheckConnectionResponse (responseStream.ToArray (), 209, Status.MalformedMessage,
                "While parsing a protocol message, the input ended unexpectedly in the middle of a field.  " +
                "This could mean either that the input has been truncated or that an embedded message misreported its own length.", 0);
        }

        [Test]
        public void ShortConnectionMessageHeader ()
        {
            var connectionMessage = new byte[5];
            Array.Copy (TestingTools.CreateConnectionRequest (Type.Rpc), connectionMessage, connectionMessage.Length);

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (connectionMessage), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var mockByteClient = new Mock<IClient<byte,byte>> ();
            mockByteClient.Setup (x => x.Stream).Returns (stream);
            var byteClient = mockByteClient.Object;

            var server = new KRPC.Server.SerialIO.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (responseStream.ToArray ().Length, 0);
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

            var server = new KRPC.Server.SerialIO.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            Assert.AreEqual (responseStream.ToArray ().Length, 0);
        }
    }
}
