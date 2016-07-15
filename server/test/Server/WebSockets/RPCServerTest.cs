using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Moq;
using KRPC.Server;
using KRPC.Test.Server;
using KRPC.Server.Message;
using KRPC.Server.WebSockets;

namespace KRPC.Test.Server.WebSockets
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
    public class RPCServerTest
    {
        [Test]
        public void ValidConnectionRequestWithNoName ()
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

            var server = new KRPC.Server.WebSockets.RPCServer (byteServer);
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

            var response = ascii.GetString (responseStream.ToArray ());
            Assert.AreEqual (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n",
                response
            );
        }

        [Test]
        public void ValidConnectionRequestWithName ()
        {
            var ascii = Encoding.ASCII;
            var request = ascii.GetBytes (
                              "GET /?name=Jebediah%20Kerman!%23%24%25%5E%26 HTTP/1.1\r\n" +
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

            var server = new KRPC.Server.WebSockets.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsTrue (eventArgs.Request.ShouldAllow);
            Assert.IsFalse (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (1, server.Clients.Count ());
            Assert.AreEqual ("Jebediah Kerman!#$%^&", server.Clients.First ().Name);

            var response = ascii.GetString (responseStream.ToArray ());
            Assert.AreEqual (
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n\r\n",
                response
            );
        }

        static string CheckInvalidConnectionRequest (string request)
        {
            return CheckInvalidConnectionRequest (Encoding.ASCII.GetBytes (request));
        }

        static string CheckInvalidConnectionRequest (byte[] request)
        {
            var responseStream = new MemoryStream ();
            var stream = new TestStream (new MemoryStream (request), responseStream);

            // Create mock byte server and client
            var mockByteServer = new Mock<IServer<byte,byte>> ();
            var byteServer = mockByteServer.Object;
            var byteClient = new TestClient (stream);

            var server = new KRPC.Server.WebSockets.RPCServer (byteServer);
            server.OnClientRequestingConnection += (sender, e) => e.Request.Allow ();
            server.Start ();

            // Fire a client connection event
            var eventArgs = new ClientRequestingConnectionEventArgs<byte,byte> (byteClient);
            mockByteServer.Raise (m => m.OnClientRequestingConnection += null, eventArgs);

            Assert.IsFalse (eventArgs.Request.ShouldAllow);
            Assert.IsTrue (eventArgs.Request.ShouldDeny);

            server.Update ();
            Assert.AreEqual (0, server.Clients.Count ());

            return Encoding.ASCII.GetString (responseStream.ToArray ());
        }

        [Test]
        public void InvalidConnectionRequestGarbage ()
        {
            var response = CheckInvalidConnectionRequest ("deadbeef".ToBytes ());
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestDuplicateFields ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongProtocolVersion ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/2\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 505 HTTP Version Not Supported\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongURI ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET /foo HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 404 Not Found\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongMethod ()
        {
            var response = CheckInvalidConnectionRequest (
                               "POST / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 13\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 405 Method Not Allowed\r\n\r\nExpected a GET request.", response);
        }

        [Test]
        public void InvalidConnectionRequestHostFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nHost field not set.", response);
        }

        [Test]
        public void InvalidConnectionRequestUpgradeFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nUpgrade field not set to websocket.", response);
        }

        [Test]
        public void InvalidConnectionRequestUpgradeFieldMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: foo\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nUpgrade field not set to websocket.", response);
        }

        [Test]
        public void InvalidConnectionRequestConnectionFieldMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nConnection field not set to Upgrade.", response);
        }

        [Test]
        public void InvalidConnectionRequestConnectionFieldMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: foo\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nConnection field not set to Upgrade.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nSec-WebSocket-Key field not set.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyTooShort ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: Zm9v\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nFailed to decode Sec-WebSocket-Key\nExpected 16 bytes, got 3 bytes.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketKeyMalformed ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: foo\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 400 Bad Request\r\n\r\nFailed to decode Sec-WebSocket-Key\nInvalid length for a Base-64 char array or string.", response);
        }

        [Test]
        public void InvalidConnectionRequestWebSocketVersionMissing ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\nSec-WebSocket-Version: 13\r\n\r\n", response);
        }

        [Test]
        public void InvalidConnectionRequestWrongWebSocketVersion ()
        {
            var response = CheckInvalidConnectionRequest (
                               "GET / HTTP/1.1\r\n" +
                               "Host: localhost\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
                               "Sec-WebSocket-Version: 12\r\n\r\n"
                           );
            Assert.AreEqual ("HTTP/1.1 426 Upgrade Required\r\nSec-WebSocket-Version: 13\r\n\r\n", response);
        }
    }
}
