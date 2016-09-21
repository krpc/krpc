using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using KRPC.Server;
using KRPC.Server.WebSockets;
using Moq;
using NUnit.Framework;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    public class StreamServerTest
    {
        [Test]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public void ValidConnectionRequest ()
        {
            var clientGuid = new Guid ("1234567890abcdef1234567890abcdef".ToBytes ());
            var clientGuidBase64 = Convert.ToBase64String (clientGuid.ToByteArray ());
            var request = Encoding.ASCII.GetBytes (
                              "GET /?id=" + clientGuidBase64 + " HTTP/1.1\r\n" +
                              "Host: localhost\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                              "Sec-WebSocket-Version: 13\r\n\r\n"
                          );

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (request), responseStream);

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

            var response = Encoding.ASCII.GetString (responseStream.ToArray ());
            Assert.AreEqual (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n",
                response
            );
        }

        [Test]
        public void InvalidConnectionRequestNoGuid ()
        {
            var ascii = Encoding.ASCII;
            var request = ascii.GetBytes (
                              "GET / HTTP/1.1\r\n" +
                              "Host: localhost\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                              "Sec-WebSocket-Version: 13\r\n\r\n"
                          );

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (request), responseStream);

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

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (0, server.Clients.Count ());

            var response = ascii.GetString (responseStream.ToArray ());
            Assert.AreEqual (
                "HTTP/1.1 400 Bad Request\r\n\r\n",
                response
            );
        }

        [Test]
        public void InvalidConnectionRequestMalformedGuid ()
        {
            var ascii = Encoding.ASCII;
            var request = ascii.GetBytes (
                              "GET /?id=foo HTTP/1.1\r\n" +
                              "Host: localhost\r\n" +
                              "Upgrade: websocket\r\n" +
                              "Connection: Upgrade\r\n" +
                              "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                              "Sec-WebSocket-Version: 13\r\n\r\n"
                          );

            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (request), responseStream);

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

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (0, server.Clients.Count ());

            var response = ascii.GetString (responseStream.ToArray ());
            Assert.AreEqual (
                "HTTP/1.1 400 Bad Request\r\n\r\n",
                response
            );
        }
    }
}
